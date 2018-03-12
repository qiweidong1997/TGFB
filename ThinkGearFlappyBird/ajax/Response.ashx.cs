using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;



namespace ThinkGearFlappyBird.ajax
{

 
    /// <summary>
    /// Response 的摘要说明
    /// </summary>
    public class Response : IHttpHandler
    {
        public const UInt32 SYNC = 0xAA;
        public const UInt32 EXCODE = 0x55;

        int tgConnection;

        //Constructor
        public Response()
        {
            //如果放到constructor里面会不会导致多次重新开始？

            //32~80 thinkgear connection
            NativeThinkgear thinkgear = new NativeThinkgear();

            /*Print driver version number */
            Console.WriteLine("Version: " + NativeThinkgear.TG_GetVersion());

            /*Get a connection ID to handle to a ThinkGear */
            int connectionID = NativeThinkgear.TG_GetNewConnectionId();
            Console.WriteLine("Connection ID: " + connectionID);

            if ( connectionID < 0)
            {
                Console.WriteLine("ERROR: TG_GetNewConnectionId() returned: " + connectionID);
                return;
            }

            int errCode = 0;
            /* Set/open stream (raw bytes) log file for connection */
            errCode = NativeThinkgear.TG_SetStreamLog(connectionID, "streamLog.txt");
            Console.WriteLine("errCode for TG_SetStreamLog : " + errCode);
            if (errCode < 0)
            {
                Console.WriteLine("ERROR: TG_SetStreamLog() returned: " + errCode);
                return;
            }

            /* Set/open data (ThinkGear values) log file for connection */
            errCode = NativeThinkgear.TG_SetDataLog(connectionID, "dataLog.txt");
            Console.WriteLine("errCode for TG_SetDataLog : " + errCode);
            if (errCode < 0)
            {
                Console.WriteLine("ERROR: TG_SetDataLog() returned: " + errCode);
                return;
            }

            /* Attempt to connect the connection ID handle to serial port "COM5" */
            //string comPortName = "\\\\.\\COM40";
            string comPortName = "\\\\.\\COM10";
            //Method TG_Connect
            errCode = NativeThinkgear.TG_Connect(connectionID,
                          comPortName,
                          NativeThinkgear.Baudrate.TG_BAUD_57600,
                          NativeThinkgear.SerialDataFormat.TG_STREAM_PACKETS);
            if (errCode < 0)
            {
                Console.WriteLine("ERROR: TG_Connect() returned: " + errCode);
                return;
            }

            //现在缺的是：
            //1.不知道constructor里面这些代码要不要放到别的method里面去
            //2.不知道如果用TG_EnableAutoRead会是什么反应
            //3.不知道要不要用TG_ReadPackets
            //4.不知道以上两个read method返回来的格式是什么样的，要怎么样get数据
            //可能需要降低游戏速度，去除上方的管子
            //除了第一个以外可能都能用 http://developer.neurosky.com/docs/doku.php?id=thinkgear_communications_protocol#thinkgear_data_values 来解决
            //用的是 eSense values (attention & mediation)
            //attention eSense outputs 1/s
            //Packet header,Payload, Checksum
            //Header 开头两个 【SYNC】（0xAA)，带一个【PLENGTH】(Payload的大小0~169）
            //The Packet's complete length will always be [PLENGTH] + 4.
            //要先verify Checksum再parse Payload
            //Checksum是用来确认“这段信息到结尾了”
            //自己先把data payload里面的值用下面的步骤算出来
            //和checksum比较，如果是一致的话才能说明这段packet到尾了
            //summing all the bytes of the Packet's Data Payload
            //taking the lowest 8 bits of the sum
            //performing the bit inverse(one's compliment inverse) on those lowest 8 bits
            //Datarow:Each DataRow contains information about what the Data Value
            //represents, the length of the Data Value, and the bytes of the Data
            //Value itself. Therefore, to parse a Data Payload, one must parse each 
            //DataRow from it until all bytes of the Data Payload have been parsed.

            //还没写Read_Packets 或者 EnableAutoRead
            /* Read 10 ThinkGear Packets from the connection, 1 Packet at a time */
            int packetsRead = 0;
            while (packetsRead < 100) //number of packets
            {

                /* Attempt to read a Packet of data from the connection */
                errCode = NativeThinkgear.TG_ReadPackets(connectionID, 1);
                Console.WriteLine("TG_ReadPackets returned: " + errCode);
                /* If TG_ReadPackets() was able to read a complete Packet of data... */
                if (errCode == 1)
                {
                    packetsRead++;

                    /* If attention value has been updated by TG_ReadPackets()... */
                    if (NativeThinkgear.TG_GetValueStatus(connectionID, NativeThinkgear.DataType.TG_DATA_RAW) != 0)
                    {

                        /* Get and print out the updated attention value */
                        Console.WriteLine("New RAW value: : " + (int)NativeThinkgear.TG_GetValue(connectionID, NativeThinkgear.DataType.TG_DATA_RAW));

                    } /* end "If attention value has been updated..." */

                } /* end "If a Packet of data was read..." */

            } /* end "Read 10 Packets of data from connection..." */

            //Code for parsing a packet and with C# IO
            string winDir = System.Environment.GetEnvironmentVariable("windir");

            //In the original c file in http://developer.neurosky.com/docs/doku.php?id=thinkgear_communications_protocol#datarow_format
            //It used to be in the main method
            int checksum = 0;
            char[] payload = new char[256]; //(?)
            char[] pLength = new char[1];
            char[] c = new char[1];
            char i;

            //open the serial data stream
            StreamReader reader = new StreamReader("COM10");

            while (true)
            {
                /* Synchronize on [SYNC] bytes */
                reader.Read(c, 1, 1);
                UInt32 cval = 0;
                cval = (UInt32)Char.GetNumericValue(c[1]);
                //if(!c.Equals(SYNC))
                if( cval != SYNC )
                    continue;

                reader.Read(c, 1, 1);
                UInt32 cval2 = 0;
                cval2 = (UInt32)Char.GetNumericValue(c[1]);
                //if (!c.Equals(SYNC))
                if( cval2 != SYNC )
                    continue;

                /* Parse [PLENGTH] byte */
                int pLengthVal = 0;

                while (true)
                {
                    reader.Read(pLength, 1, 1);
                    pLengthVal = (int)Char.GetNumericValue(pLength[1]);
                    //int val = int.TryParse(pLength);
                    if (pLengthVal != 170)
                        break;
                }
                if (pLengthVal > 169)
                    continue;

                /* Collect [PAYLOAD...] bytes */
                reader.Read(payload, 1, pLengthVal);

                /* Compute [PAYLOAD...] chksum */

                for( int x=0; x<pLengthVal; x++)
                {
                    checksum = checksum + payload[x];
                }
                checksum &= 0xFF;
                checksum = ~checksum & 0xFF;

                /* Parse [CKSUM] byte */
                reader.Read(c, 1, 1);

                /* Verify [PAYLOAD...] checksum against [CKSUM] */
                // c value 3
                UInt32 cval3 = 0;
                cval3 = (UInt32)Char.GetNumericValue(c[1]);
                if (cval3 != checksum)
                    continue;

                /* Since [CKSUM] is OK, parse the Data Payload */
                //OH MY GOD C SHARP……
                parsePayload(payload, pLengthVal);

            }


        }

        public int parsePayload( char [] payload, int pLength)
        {
            //In C# the same data type for unsigned char (in C++) is byte(8 bit) or char（16b)

            int bytesParsed = 0;        //used to be char
            char code;
            int length;
            int extendedCodeLevel = 0;  //used to be char
            int i;

            /* Loop until all bytes are parsed from the payload[] array... */
            while (bytesParsed < pLength)
            {
                /* Parse the extendedCodeLevel, code, and length */
                while(payload[bytesParsed] == EXCODE)
                {
                    extendedCodeLevel++;
                    bytesParsed++;
                }

                code = payload[bytesParsed++];
                
                if ((code & 0x80) == 1) //possible error for bit operation "code" is 2bytes
                    length = payload[bytesParsed++];
                else
                    length = 1;

                /* TODO: Based on the extendedCodeLevel, code, length,
                 * and the [CODE] Definitions Table, handle the next
                 * "length" bytes of data from the payload as
                 * appropriate for your application.
                 */
                 //POSSIBLE ERROR TODO
                Console.WriteLine("EXCODE level: {0} CODE:0x{0:X2} length: {0}\r\n",
                                   extendedCodeLevel, code, length);
                Console.WriteLine("Data value(s):");
                for( i=0; i<length; i++)
                {
;                    Console.WriteLine("{0:X2}", payload[bytesParsed + i] & 0xFF);
                }
                Console.WriteLine("\n");
            }
            return 0;
        }

        private void addListItem(string value)
        {
            this.listBox1.Items.Add(value);
            //但是并没有listBox1？
            //Note Instead of declaring and using the addListItem function, 
            //you can use the following statement directly:
            //this.listBox1.Items.Add(value); "
        }

        //Disconnect method
        public void Disconnect(int connectionID)
        {
            NativeThinkgear.TG_Disconnect(connectionID); // disconnect test

            /* Clean up */
            NativeThinkgear.TG_FreeConnection(connectionID);

            /* End program */
            Console.ReadLine();
        }

        /// <summary>
        /// 默认的后端方法ProcessRequest，处理Default.aspx的请求
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            context.Response.Write(GetState() ? "1" : "0"); //虚假回复 dummy response
            //context.Response.Write(Response() ? "1" : "0")
            
            
        }

        /// <summary>
        /// 模拟一个虚假的回复，用上NativeThinkgear之后可以删掉
        /// </summary>
        /// <returns></returns>
        public bool GetState()
        {
            Random r = new Random();
            double rand = r.NextDouble();
            return rand > 0.3;
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}