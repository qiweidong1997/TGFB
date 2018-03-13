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
        public static Response rep = null;
        //int tgConnection;
        static int connectionID = -1;
        static bool initialized = false;

        //Constructor
        public static Response constructor()
        {
            if(rep== null)
            {
                rep = new Response();
                rep.initialize();
            }
            return rep;
        }

        void initialize()
        {
            //如果放到constructor里面会不会导致多次重新开始？
            //32~80 thinkgear connection
            NativeThinkgear thinkgear = new NativeThinkgear();

            /*Print driver version number */
            Console.WriteLine("Version: " + NativeThinkgear.TG_GetVersion());

            /*Get a connection ID to handle to a ThinkGear */
            connectionID = NativeThinkgear.TG_GetNewConnectionId();
            Console.WriteLine("Connection ID: " + connectionID);

            if (connectionID < 0)
            {
                Console.WriteLine("ERROR: TG_GetNewConnectionId() returned: " + connectionID);
                return;
            }

            int errCode = 0;
            ///* Set/open stream (raw bytes) log file for connection */
            //errCode = NativeThinkgear.TG_SetStreamLog(connectionID, "streamLog.txt");
            //Console.WriteLine("errCode for TG_SetStreamLog : " + errCode);
            //if (errCode < 0)
            //{
            //    Console.WriteLine("ERROR: TG_SetStreamLog() returned: " + errCode);
            //    return;
            //}
            //
            ///* Set/open data (ThinkGear values) log file for connection */
            //errCode = NativeThinkgear.TG_SetDataLog(connectionID, "dataLog.txt");
            //Console.WriteLine("errCode for TG_SetDataLog : " + errCode);
            //if (errCode < 0)
            //{
            //    Console.WriteLine("ERROR: TG_SetDataLog() returned: " + errCode);
            //    return;
            //}

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

        }

        public Response()
        {
            
            
        }

        /*
         * This function reads the attention input (copied from example c# 64 code)
         * and averages it, since there are so many strange values, we want a more stable output
         */
        public int readAndAverageAttentionInput(int connectionID)
        {
            /* Read 10 ThinkGear Packets from the connection, 1 Packet at a time */
            int packetsRead = 0;
            int packetsValue = 0;

            int errCode = 0;

            while (packetsRead < 100) //number of packets
            {

                /* Attempt to read a Packet of data from the connection */
                errCode = NativeThinkgear.TG_ReadPackets(connectionID, 1);
                Console.WriteLine("TG_ReadPackets returned: " + errCode);
                /* If TG_ReadPackets() was able to read a complete Packet of data... */
                if (errCode == 1)
                {
                    packetsRead++;

                    //QUESTION: is this attracted value ACTUALLY Attention Value?
                    //QUESTION: when using thinkgear connector test csharp 64, the response value will always be 53 or 54 when errCode = 1 if the while loop is less than 1000, what causes that?
                    /* If attention value has been updated by TG_ReadPackets()... */
                    if (NativeThinkgear.TG_GetValueStatus(connectionID, NativeThinkgear.DataType.TG_DATA_RAW) != 0)
                    {

                        packetsValue += (int)NativeThinkgear.TG_GetValue(connectionID, NativeThinkgear.DataType.TG_DATA_ATTENTION);
                        /* Get and print out the updated attention value */
                        //The original sentence
                        //Console.WriteLine("New RAW value: : " + (int)NativeThinkgear.TG_GetValue(connectionID, NativeThinkgear.DataType.TG_DATA_RAW));

                    } /* end "If attention value has been updated..." */

                } /* end "If a Packet of data was read..." */

            } /* end "Read 10 Packets of data from connection..." */

            return ((int)(packetsValue / packetsRead));

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

        ~Response()
        {
            //Disconnect(connectionID);
        }

        /*
         * 后端process前端的request的function
         */
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            context.Response.Write(GetState());
            
        }
        

        public int GetState()
        {
            Response rep = Response.constructor();
            int result = rep.readAndAverageAttentionInput(connectionID);
            if (initialized)
            {

            }
            return result;
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