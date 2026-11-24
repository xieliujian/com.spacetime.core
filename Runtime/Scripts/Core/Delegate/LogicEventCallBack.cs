namespace ST.Core
{
    /// <summary>动态命令回调，参数为命令 ID 与调用方传入的参数列表。</summary>
    /// <param name="commandID">触发的动态命令 ID。</param>
    /// <param name="args">命令参数，无参数时为 null 或空列表。</param>
    public delegate void LogicEventCallBack(int commandID, IVarList args);
}
