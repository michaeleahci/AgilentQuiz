namespace AgilentQuiz.Application.Common;

/// <summary>
/// 统一错误码定义
/// </summary>
public static class ErrorCodes
{
    // 通用
    public const string Success = "0";
    public const string InvalidParameter = "1001";
    public const string ResourceNotFound = "1002";
    public const string Unauthorized = "1003";

    // 预约相关
    public const string ReservationTooLate = "2001";           // 预约必须提前1小时
    public const string UserBanned = "2002";                    // 用户在违约禁用期
    public const string InstrumentTypeDisabled = "2003";        // 仪器类型已禁用
    public const string InstrumentNotAvailable = "2004";        // 仪器不可用（故障/报废）
    public const string InstrumentConflict = "2005";            // 仪器时间段冲突
    public const string ReservationNotFound = "2006";           // 预约单不存在
    public const string ReservationCannotCancel = "2007";      // 预约单不可取消
    public const string DuplicateIdempotencyKey = "2008";       // 幂等键重复（直接返回原结果）

    // 仪器/类型相关
    public const string InstrumentTypeNotFound = "3001";
    public const string InstrumentNotFound = "3002";
    public const string InstrumentTypeCodeExists = "3003";
    public const string InstrumentCodeExists = "3004";
    public const string CannotScrapActiveReservation = "3005"; // 报废时存在有效预约
}
