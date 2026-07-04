using Taipi.Core.Exceptions;

namespace Taipi.Core.Assertions
{
    public static class AsseterExtensions
    {
        /// <summary>
        /// 断言为False时抛出异常
        /// </summary>
        /// <param name="r">布尔值</param>
        /// <param name="errorCode">错误码</param>
        /// <param name="errorMsg">错误信息</param>
        public static void MustBeFalse(this bool r, int errorCode, string errorMsg)
        {
            if (!r)
            {
                throw new AppException(errorCode, errorMsg);
            }
        }

        /// <summary>
        /// 断言为True时抛出异常
        /// </summary>
        /// <param name="r">布尔值</param>
        /// <param name="errorCode">错误码</param>
        /// <param name="errorMsg">错误信息</param>
        public static void MustBeTrue(this bool r, int errorCode, string errorMsg)
        {
            if (r)
            {
                throw new AppException(errorCode, errorMsg);
            }
        }

        /// <summary>
        /// 如果为真, 则执行操作
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