from sqlalchemy.ext.asyncio import AsyncEngine, AsyncSession, async_sessionmaker, create_async_engine


def normalize_connection_string(connection_string: str) -> str:
    if connection_string.startswith("postgresql://"):
        return connection_string.replace("postgresql://", "postgresql+psycopg://", 1)
    if connection_string.startswith("postgresql+psycopg://"):
        return connection_string
    raise ValueError("vector connection string must use PostgreSQL with Psycopg")


def create_database_engine(connection_string: str, *, echo: bool = False) -> AsyncEngine:
    return create_async_engine(
        normalize_connection_string(connection_string),
        echo=echo,
        pool_pre_ping=True,
    )


def create_session_factory(engine: AsyncEngine) -> async_sessionmaker[AsyncSession]:
    return async_sessionmaker(engine, expire_on_commit=False)
