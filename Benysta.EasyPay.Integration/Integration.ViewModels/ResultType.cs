
using System.ComponentModel;

namespace Integration.ViewModels;

public enum ResultType
{
    [Description("Success")] Success = 1,
    [Description("Error")] Error = 2,
    [Description("ValidationError")] ValidationError = 3,
    [Description("Warning")] Warning = 4,
    [Description("CustomerInformation")] CustomerInformation = 5,
    [Description("Empty")] Empty = 6
}
