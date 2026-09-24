using System.Text;

namespace Common.Persistence;

public static class EnumExtensions
{
    public static string BuildDescription<TEnum>() where TEnum : Enum
    {
        var result = new StringBuilder();

        // Keep migrations identical across operating systems.
        result.Append($"Possible values of enum type: {typeof(TEnum).Name}\n");
        result.Append("----------\n");

        foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
        {
            result.Append($"{Convert.ToInt32(value)} -- {value}\n");
        }

        return result.ToString();
    }
}
