namespace Common.Constants;

public static class KafkaConstants
{
    public const string UserRegisteredTopic = "users.registered.v1";
    public const string CommissionsPaidTopic = "commissions.paid.v1";
    public const string CommissionsAccruedTopic = "commissions.accrued.v1";

    public static IReadOnlyList<string> AllTopics { get; } =
        Array.AsReadOnly<string>(
        [
            UserRegisteredTopic,
            CommissionsPaidTopic,
            CommissionsAccruedTopic
        ]);
}
