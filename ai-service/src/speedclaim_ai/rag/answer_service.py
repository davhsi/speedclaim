from dataclasses import replace

from speedclaim_ai.config.settings import POLICY_QA_PROMPT_VERSION
from speedclaim_ai.providers.chat.base import (
    ChatProvider,
    ChatProviderError,
    ChatProviderRateLimited,
    ChatProviderResponseError,
    ChatProviderTimeout,
    ChatProviderUnavailable,
)
from speedclaim_ai.rag.citations import InvalidGroundedAnswer, validate_generated_answer
from speedclaim_ai.rag.errors import PolicyQaFailure
from speedclaim_ai.rag.models import PolicyQaCommand, PolicyQaResult
from speedclaim_ai.rag.prompts import build_policy_qa_request
from speedclaim_ai.rag.retrieval_service import RetrievalService
from speedclaim_ai.repositories.vector_repository import VectorRepository
from speedclaim_ai.security.input_validation import QuestionValidationError

_INSUFFICIENT_ANSWER = (
    "I couldn’t find enough evidence in this brochure to answer that question. "
    "Please check the policy document or contact SpeedClaim support."
)


class PolicyQaService:
    def __init__(
        self,
        *,
        repository: VectorRepository,
        retrieval_service: RetrievalService,
        chat_provider: ChatProvider,
        prompt_version: str = POLICY_QA_PROMPT_VERSION,
        validation_attempts: int = 2,
    ) -> None:
        if validation_attempts not in {1, 2}:
            raise ValueError("answer validation attempts must be one or two")
        self._repository = repository
        self._retrieval_service = retrieval_service
        self._chat_provider = chat_provider
        self._prompt_version = prompt_version
        self._validation_attempts = validation_attempts

    async def answer(self, command: PolicyQaCommand) -> PolicyQaResult:
        document = await self._repository.get_document_by_brochure_id(command.brochure_id)
        if document is None:
            raise PolicyQaFailure(
                code="brochure_not_indexed",
                message="The requested brochure has not been indexed.",
                status_code=404,
            )
        if (
            document.product_id != command.product_id
            or document.brochure_version != command.brochure_version
        ):
            raise PolicyQaFailure(
                code="brochure_metadata_mismatch",
                message="The requested brochure metadata does not match the indexed version.",
                status_code=409,
            )

        try:
            retrieval = await self._retrieval_service.retrieve(
                command.brochure_id,
                command.question,
            )
        except QuestionValidationError as exc:
            raise PolicyQaFailure(
                code="invalid_question",
                message=str(exc),
                status_code=422,
            ) from exc

        if retrieval.evidence_status == "InsufficientEvidence":
            return PolicyQaResult(
                request_id=command.request_id,
                answer=_INSUFFICIENT_ANSWER,
                evidence_status="InsufficientEvidence",
                brochure_version=document.brochure_version,
                citations=(),
                prompt_version=self._prompt_version,
                provider=None,
                model=None,
            )

        chat_request = build_policy_qa_request(
            prompt_version=self._prompt_version,
            normalized_question=retrieval.normalized_question,
            evidence=retrieval.evidence,
        )
        last_validation_error: InvalidGroundedAnswer | None = None
        for _attempt in range(self._validation_attempts):
            completion = await self._complete(chat_request)
            try:
                validated = validate_generated_answer(
                    completion.content,
                    retrieval.evidence,
                )
            except InvalidGroundedAnswer as exc:
                last_validation_error = exc
                chat_request = replace(
                    chat_request,
                    system_prompt=(
                        f"{chat_request.system_prompt}\n\n"
                        "The previous response failed application validation. Return a fresh "
                        "schema-compliant response whose citation IDs exist and whose support "
                        "quotes are copied exactly from their cited evidence."
                    ),
                )
                continue

            evidence_status = {
                "Grounded": "Grounded",
                "InsufficientEvidence": "InsufficientEvidence",
                "UnsupportedRequest": "Rejected",
            }[validated.answer_type]
            return PolicyQaResult(
                request_id=command.request_id,
                answer=validated.answer,
                evidence_status=evidence_status,
                brochure_version=document.brochure_version,
                citations=validated.citations,
                prompt_version=self._prompt_version,
                provider=completion.provider,
                model=completion.model,
            )

        raise PolicyQaFailure(
            code="invalid_model_output",
            message="The answer provider returned an unsupported or malformed answer.",
            status_code=503,
        ) from last_validation_error

    async def _complete(self, chat_request):
        try:
            return await self._chat_provider.complete(chat_request)
        except ChatProviderRateLimited as exc:
            raise PolicyQaFailure(
                code="chat_provider_rate_limited",
                message="The answer provider is rate limited. Please retry later.",
                status_code=429,
                retry_after_seconds=exc.retry_after_seconds,
            ) from exc
        except ChatProviderTimeout as exc:
            raise PolicyQaFailure(
                code="chat_provider_timeout",
                message="The answer provider timed out. Please retry.",
                status_code=503,
            ) from exc
        except ChatProviderUnavailable as exc:
            raise PolicyQaFailure(
                code="chat_provider_unavailable",
                message="The answer provider is temporarily unavailable.",
                status_code=503,
            ) from exc
        except (ChatProviderResponseError, ChatProviderError) as exc:
            raise PolicyQaFailure(
                code="chat_provider_error",
                message="The answer provider could not complete the request.",
                status_code=503,
            ) from exc
