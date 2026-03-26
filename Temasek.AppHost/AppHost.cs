var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env");

var db = builder
    .AddMySql("db")
    .WithDataVolume("temasek-data")
    .WithPhpMyAdmin()
    .AddDatabase("temasek");

var clerkSecretKey = builder.AddParameter("clerk-secret-key", true);
var clerkPublishableKey = builder.AddParameter("clerk-publishable-key");
var clerkAuthorizedParties = builder.AddParameter("clerk-authorized-parties");
var formSgSecretKey = builder.AddParameter("formsg-secret-key", secret: true);
var formSgCallbackApiKey = builder.AddParameter("formsg-callback-api-key", secret: true);

var calendarrEnabled = builder.AddParameter("calendarr-enabled", "true");
var calendarrServiceAccountJsonCredential = builder.AddParameter(
    "calendarr-service-account-json-credential",
    secret: true
);
var calendarrSyncParentCalendarId = builder.AddParameter("calendarr-sync-parent-calendar-id");
var calendarrSyncChildCalendarId = builder.AddParameter("calendarr-sync-child-calendar-id");
var calendarrSyncInterval = builder.AddParameter("calendarr-sync-interval", "1:0:0");
var calendarrBdeComdSourceCalendarId = builder.AddParameter(
    "calendarr-bde-comd-source-calendar-id"
);
var calendarrBdeComdTargetCalendarId = builder.AddParameter(
    "calendarr-bde-comd-target-calendar-id"
);
var calendarrBdeComdSyncInterval = builder.AddParameter(
    "calendarr-bde-comd-sync-interval",
    "1:0:0"
);

var facilitiesEnabled = builder.AddParameter("facilities-enabled", "true");
var facilitiesServiceAccountJsonCredential = builder.AddParameter(
    "facilities-service-account-json-credential",
    secret: true
);
var facilitiesCalendarId = builder.AddParameter("facilities-calendar-id");
var facilitiesCarbonCopyCalendarId = builder.AddParameter("facilities-carbon-copy-calendar-id");

var api = builder
    .AddProject<Projects.Temasek_WebApi>("temasek-webapi")
    .WithReference(db)
    .WaitFor(db)
    .WithEnvironment("FormSg:CallbackApiKey", formSgCallbackApiKey)
    .WithEnvironment("FormSg:SecretKey", formSgSecretKey)
    .WithEnvironment("Clerk:SecretKey", clerkSecretKey)
    .WithEnvironment("Clerk:AuthorizedParties", clerkAuthorizedParties)
    .WithEnvironment("Calendarr:Enabled", calendarrEnabled)
    .WithEnvironment(
        "Calendarr:ServiceAccountJsonCredential",
        calendarrServiceAccountJsonCredential
    )
    .WithEnvironment("Calendarr:SyncParentCalendarId", calendarrSyncParentCalendarId)
    .WithEnvironment("Calendarr:SyncChildCalendarId", calendarrSyncChildCalendarId)
    .WithEnvironment("Calendarr:SyncInterval", calendarrSyncInterval)
    .WithEnvironment("Calendarr:BdeComdSourceCalendarId", calendarrBdeComdSourceCalendarId)
    .WithEnvironment("Calendarr:BdeComdTargetCalendarId", calendarrBdeComdTargetCalendarId)
    .WithEnvironment("Calendarr:BdeComdSyncInterval", calendarrBdeComdSyncInterval)
    .WithEnvironment("Facilities:Enabled", facilitiesEnabled)
    .WithEnvironment(
        "Facilities:ServiceAccountJsonCredential",
        facilitiesServiceAccountJsonCredential
    )
    .WithEnvironment("Facilities:CalendarId", facilitiesCalendarId)
    .WithEnvironment("Facilities:CarbonCopyCalendarId", facilitiesCarbonCopyCalendarId);

var app = builder
    .AddViteApp("temasek-webapp", "../Temasek.WebApp")
    .WithPnpm()
    .PublishAsDockerFile()
    .PublishAsDockerComposeService((_, _) => { })
    .WithReference(api)
    .WithEnvironment("NUXT_PUBLIC_CLERK_PUBLISHABLE_KEY", clerkPublishableKey);

builder.Build().Run();
