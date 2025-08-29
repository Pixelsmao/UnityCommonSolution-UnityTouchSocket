using TouchSocket.Core;

namespace Assets.Scenes._2D.Script
{
    /// <summary>
    /// Mono容器服务构建
    /// </summary>
    public static class ContainerService
    {

        public static Container container { get; private set; } = new Container();
        private static IResolver resolver;

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static T Resolve<T>() where T : class
        {
            if (resolver == null)
            {
                resolver = container.BuildResolver();
            }
            return resolver.Resolve<T>();
        }


    }
}
