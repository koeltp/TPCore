using Taipi.Core.Exceptions;

namespace Taipi.Core.Assertions
{
    public static class AssertExtensions
    {
        /// <summary>
        /// 值为 false 时抛出 AppException，为 true 时安全通过
        /// </summary>
        /// <param name="r">布尔值</param>
        /// <param name="errorCode">错误码</param>
        /// <param name="errorMsg">错误信息</param>
        public static void ThrowIfFalse(this bool r, int errorCode, string errorMsg)
        {
            if (!r)
            {
                throw new AppException(errorCode, errorMsg);
            }
        }
        /// <summary>
        /// 值为 false 时抛出 AppException，为 true 时安全通过
        /// </summary>
        /// <param name="r">布尔值</param>
        /// <param name="exception">异常</param>
        public static void ThrowIfFalse(this bool r, AppException exception)
        {
            if (!r)
            {
                throw exception;
            }
        }
        /// <summary>
        /// 值为 true 时抛出 AppException，为 false 时安全通过
        /// </summary>
        /// <param name="r">布尔值</param>
        /// <param name="errorCode">错误码</param>
        /// <param name="errorMsg">错误信息</param>
        public static void ThrowIfTrue(this bool r, int errorCode, string errorMsg)
        {
            if (r)
            {
                throw new AppException(errorCode, errorMsg);
            }
        }
        /// <summary>
        /// 值为 true 时抛出 AppException，为 false 时安全通过
        /// </summary>
        /// <param name="r">布尔值</param>
        /// <param name="exception">异常</param>
        public static void ThrowIfTrue(this bool r, AppException exception)
        {
            if (r)
            {
                throw exception;
            }
        }

        /// <summary>
        /// 值为 true 时执行操作，否则跳过
        /// </summary>
        /// <param name="predicates">布尔值</param>
        /// <param name="action">操作</param>
        public static void ExecuteIfTrue(this bool predicates, Action action)
        {
            if (predicates)
            {
                action.Invoke();
            }
        }
    }
}