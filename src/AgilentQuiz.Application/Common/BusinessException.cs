namespace AgilentQuiz.Application.Common;

/// <summary>
/// 业务异常
/// </summary>
public class BusinessException : Exception
{
    public string Code { get; }

    public BusinessException(string code, string message) : base(message)
    {
        Code = code;
    }
}
