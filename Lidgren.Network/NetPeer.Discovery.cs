﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

#if !__NOIPENDPOINT__
using NetEndPoint = System.Net.IPEndPoint;
#endif

namespace Lidgren.Network
{
	public partial class NetPeer
	{
		/// <summary>
		/// Emit a discovery signal to all hosts on your subnet
		/// </summary>
		public void DiscoverLocalPeers(int serverPort, NetOutgoingMessage message = null)
		{
            message = message ?? CreateMessage(0);

            message.m_messageType = NetMessageType.Discovery;
			Interlocked.Increment(ref message.m_recyclingCount);

			var broadcastAddress = NetUtility.GetBroadcastAddress();
			if (m_configuration.DualStack)
				broadcastAddress = NetUtility.MapToIPv6(broadcastAddress);

            m_unsentUnconnectedMessages.Enqueue(new NetTuple<NetEndPoint, NetOutgoingMessage>(new NetEndPoint(broadcastAddress, serverPort), message));
		}

		/// <summary>
		/// Emit a discovery signal to a single known host
		/// </summary>
		public bool DiscoverKnownPeer(string host, int serverPort, NetOutgoingMessage message = null)
		{
			var address = NetUtility.Resolve(host);
			if (address == null)
				return false;
			DiscoverKnownPeer(new NetEndPoint(address, serverPort), message);
			return true;
		}

		/// <summary>
		/// Emit a discovery signal to a single known host
		/// </summary>
		public void DiscoverKnownPeer(NetEndPoint endPoint, NetOutgoingMessage message = null)
		{
			if (endPoint == null)
				throw new ArgumentNullException(nameof(endPoint));
			if (m_configuration.DualStack)
				endPoint = NetUtility.MapToIPv6(endPoint);

			message = message ?? CreateMessage(0);

			message.m_messageType = NetMessageType.Discovery;
			message.m_recyclingCount = 1;
			m_unsentUnconnectedMessages.Enqueue(new NetTuple<NetEndPoint, NetOutgoingMessage>(endPoint, message));
		}

		/// <summary>
		/// Send a discovery response message
		/// </summary>
		public void SendDiscoveryResponse(NetOutgoingMessage msg, NetEndPoint recipient)
		{
			if (recipient == null)
				throw new ArgumentNullException("recipient");

			if (msg == null)
				msg = CreateMessage(0);
			else if (msg.m_isSent)
				throw new NetException("Message has already been sent!");

			if (msg.LengthBytes >= m_configuration.MaximumTransmissionUnit)
				throw new NetException("Cannot send discovery message larger than MTU (currently " + m_configuration.MaximumTransmissionUnit + " bytes)");

			msg.m_messageType = NetMessageType.DiscoveryResponse;
			Interlocked.Increment(ref msg.m_recyclingCount);
			m_unsentUnconnectedMessages.Enqueue(new NetTuple<NetEndPoint, NetOutgoingMessage>(recipient, msg));
		}
	}
}
