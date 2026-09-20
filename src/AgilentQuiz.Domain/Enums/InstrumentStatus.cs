namespace AgilentQuiz.Domain.Enums;

/// <summary>
/// 仪器状态
/// </summary>
public enum InstrumentStatus
{
    /// <summary>
    /// 可用
    /// </summary>
    Available = 1,

    /// <summary>
    /// 故障
    /// </summary>
    Faulty = 2,

    /// <summary>
    /// 报废
    /// </summary>
    Scrapped = 3
}
