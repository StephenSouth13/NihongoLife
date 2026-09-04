namespace NihongoLife.Core
{
    public readonly struct OnlineDatabaseSettings
    {
        public readonly bool Enabled;
        public readonly string Host;
        public readonly int Port;
        public readonly string Database;
        public readonly string User;
        public readonly string PasswordEnvironmentKey;
        public readonly bool RequireSsl;

        public OnlineDatabaseSettings(GameControlDatabase database)
        {
            Enabled = database != null && database.enableOnlineSync;
            Host = database != null ? database.supabaseHost : string.Empty;
            Port = database != null ? database.postgresPort : 5432;
            Database = database != null ? database.databaseName : string.Empty;
            User = database != null ? database.userName : string.Empty;
            PasswordEnvironmentKey = database != null ? database.passwordEnvironmentKey : "NIHONGOLIFE_POSTGRES_PASSWORD";
            RequireSsl = database == null || database.requireSsl;
        }
    }
}
