//------------------------------------------------------------------------------
//  此代码版权归作者本人若汝棋茗所有
//  源代码使用协议遵循本仓库的开源协议及附加协议，若本仓库没有设置，则按MIT开源协议授权
//  CSDN博客：https://blog.csdn.net/qq_40374647
//  哔哩哔哩视频：https://space.bilibili.com/94253567
//  Gitee源代码仓库：https://gitee.com/RRQM_Home
//  Github源代码仓库：https://github.com/RRQM
//  交流QQ群：234762506
//  感谢您的下载和使用
//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
using RRQMCore.ByteManager;
using RRQMSocket;
using System.Net;

/// <summary>
/// 显示信息
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
public delegate void RRQMMessageEventHandler(object sender, MesEventArgs e);

/// <summary>
/// ByteBlock
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
public delegate void RRQMByteBlockEventHandler(object sender, ByteBlock e);

/// <summary>
/// UDP接收
/// </summary>
/// <param name="endpoint"></param>
/// <param name="e"></param>
public delegate void RRQMUDPByteBlockEventHandler(EndPoint endpoint, ByteBlock e);

/// <summary>
/// 字节数据
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
public delegate void RRQMBytesEventHandler(object sender, BytesEventArgs e);

/// <summary>
/// 字节数据
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
public delegate void RRQMReturnBytesEventHandler(object sender, ReturnBytesEventArgs e);