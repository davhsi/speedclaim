using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Financial;
using SpeedClaim.Api.Dtos.Payments;

namespace SpeedClaim.Api.Interfaces;

public interface IFinanceService
{
    // Customer
    Task<CreatePaymentIntentResponse> PayPremiumAsync(string customerId, string scheduleId, CreatePaymentIntentRequest request);
    Task<IEnumerable<PremiumScheduleDto>> GetPremiumScheduleAsync(string policyId, string customerId);
    Task<IEnumerable<PaymentRecordDto>> GetMyPaymentHistoryAsync(string customerId);
    Task<PaymentRecordDto> DownloadReceiptAsync(string paymentId, string customerId);
    Task<IEnumerable<SavedCardDto>> GetSavedPaymentMethodsAsync(string customerId);

    // Finance Officer
    Task<IEnumerable<PaymentRecordDto>> GetAllPaymentRecordsAsync();
    Task ReconcilePaymentAsync(string paymentId, string financeOfficerId);
    Task ReconcileByStripeIntentAsync(string paymentIntentId);
    Task ProcessRefundAsync(string paymentId, string financeOfficerId);
    Task ProcessClaimPayoutAsync(string claimId, string financeOfficerId);
    Task MarkClaimFinanciallySettledAsync(string claimId, string financeOfficerId);
    Task<IEnumerable<AgentCommissionDto>> GetPendingCommissionsAsync();
    Task ApproveAndPayCommissionAsync(string commissionId, string financeOfficerId);

    // Reports
    Task<IEnumerable<PremiumScheduleDto>> GetOverduePoliciesAsync();
    Task<PaymentSummaryDto> GetPremiumCollectionSummaryAsync(string period);
    Task<byte[]> ExportPaymentReportsAsync();
}
