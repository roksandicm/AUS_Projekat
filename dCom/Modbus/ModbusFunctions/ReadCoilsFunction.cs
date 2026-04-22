using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
		public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
        {
            ModbusReadCommandParameters p = (ModbusReadCommandParameters)CommandParameters;
            byte[] request = new byte[12];

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Length)), 0, request, 4, 2);
            request[6] = p.UnitId;
            request[7] = p.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.StartAddress)), 0, request, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Quantity)), 0, request, 10, 2);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            if (response[7] == (byte)(CommandParameters.FunctionCode + 0x80))
            {
                HandeException(response[8]);
            }

            ModbusReadCommandParameters readParameters = (ModbusReadCommandParameters)CommandParameters;
            ushort startAddress = readParameters.StartAddress;
            int count = 0;
            int byteCount = response[8];
            ushort mask = 1;

            for (int i = 0; i < byteCount; i++)
            {
                byte temp = response[9 + i];

                for (int j = 0; j < 8; j++)
                {
                    ushort value = (ushort)((temp & mask) != 0 ? 1 : 0);
                    result.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, startAddress), value);

                    temp >>= 1;
                    count++;
                    startAddress++;

                    if (count >= readParameters.Quantity)
                    {
                        break;
                    }
                }
            }

            return result;
        }
    }
}