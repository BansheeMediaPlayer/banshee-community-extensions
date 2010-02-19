using System;
using System.Runtime.InteropServices;

namespace Lirc
{
    public class LircClient
    {
        private string program_name;
        private bool is_connected;
        
        public string ProgramName {
            get {
                return program_name;
            }
            set {
                program_name = value;
            }
        }
        
        public LircClient (string prog)
        {
            program_name = prog;
            Connect();
        }
        
        public string NextCommand ()
        {
            // FIXME:
            // in the future, this method should handle errors and detect a dead server, 
            // disconnect from it, and initialize a reconnect routine - which is also yet
            // to be written... :)

            IntPtr ret = IntPtr.Zero;
            ret = lirc_glue_next_valid_command ();

            string command;
            
            switch (lirc_glue_get_error ()) {
                case 0:
                    if(ret.ToInt32() != -1) {
                        command = Marshal.PtrToStringAuto(ret);
                    } else {
                        command = null;
                        ErrorValue = -4;
                    }
                    break;
                case -1:
                    command = null;
                    break;
                default:
                    command = null;
                    Console.WriteLine("lirc-sharp: unhandled return value of {0}", ret);
                    break;
            }
            return (command);
        }
        
        public int ErrorValue {
            get {
                return (lirc_glue_get_error ());
            }
            set {
                lirc_glue_set_error (value);
            }
        }
        
        public string NextCode ()
        {
            string code;
            if (lirc_nextcode (out code) == 0)
                return (code);
            else
                return (null);
        }
        
//        public LircConfig Config {
//            get {
//                LircConfig config = new LircConfig ();
//                IntPtr ptr;
//                ptr = lirc_glue_get_config ();
//                if (ptr != IntPtr.Zero) {
//                    Marshal.PtrToStructure (ptr, config);
//                } else {
//                    Console.WriteLine("lirc-sharp: unable to read configuration.");
//                }
//                return (config);
//            }
//        }
        
        public bool Connect() {
            if(!is_connected) {
                if (lirc_init (program_name, 1) == -1) {
                    Console.WriteLine("lirc-sharp: lirc_init() failed");
                    this.ErrorValue = -2;
                    return(false);
                }
    
                if(lirc_glue_readconfig () != 0) {
                    Console.WriteLine("lirc-sharp: some sort of error on readconfig");
                    this.ErrorValue = -1;
                    return(false);
                }
                is_connected = true;
                ErrorValue = 0;
            }
            return(true);
        }

        public void Disconnect() {
            if(is_connected) {
                Console.Write("lirc-sharp: Disconnecting LIRC connection...");
                lirc_glue_freeconfig ();
                lirc_deinit ();
                ErrorValue = -3;
                Console.WriteLine("done.");
                is_connected = false;
            }
        }

        public void ReConnect() {
            Disconnect();
            Connect();
        }

        public void Dispose () {
            Disconnect();
        }


    /* .......................lirc_glue bindings.................................................*/
    [DllImport("liblircglue")]
    private extern static int lirc_glue_readconfig ();

    [DllImport("liblircglue")]
    private extern static IntPtr lirc_glue_next_valid_command ();

    [DllImport("liblircglue")]
    private extern static IntPtr lirc_glue_get_config ();

    [DllImport("liblircglue")]
    private extern static void lirc_glue_freeconfig();
    
    [DllImport("liblircglue")]
    private extern static int lirc_glue_get_error ();

    [DllImport("liblircglue")]
    private extern static int lirc_glue_set_error (int errorvalue);

    /* .......................lirc_client bindings.................................................*/
    
    [DllImport("lirc_client")]
    private extern static int lirc_init(string prog, int verbose);

    [DllImport("lirc_client")]
    private extern static int lirc_deinit();

    [DllImport("lirc_client")]
    private extern static int lirc_nextcode(out string code);

    /*[DllImport("lirc_client")]
    private extern static int lirc_readconfig(string file, out Config config, string check);

    [DllImport("lirc_client")]
    private extern static int lirc_freeconfig(out Config config);

    [DllImport("lirc_client")]
    private extern static int lirc_code2char(out Config config, string code, out string str);*/


    /* someone write bindings for client daemon functions?  check out lirc_client.h */



    }
   public enum LircFlags
   {
       none = 0x00,
       once = 0x01,
       quit = 0x02,
       mode = 0x04,
       ecno = 0x08,
       startup_mode = 0x10
   }
   
   [StructLayout(LayoutKind.Sequential)]
   public unsafe struct LircList
   {
       public string str;
       public IntPtr next;
   }

   [StructLayout(LayoutKind.Sequential)]
   public unsafe struct LircCode
   {
       public string remote;
       public string button;
       public IntPtr next;
   }

   [StructLayout(LayoutKind.Sequential)]
   public unsafe struct LircConfig
   {
       public string current_mode;
       public IntPtr next;
       public IntPtr first;
       public int sockfd;
   }
   
   [StructLayout(LayoutKind.Sequential)]
   public unsafe struct LircConfigEntry
   {
       public string prog;
       public IntPtr code;
       public uint rep_delay;
       public uint rep;
       public IntPtr config;
       public string change_mode;
       public uint flags;
       
       public string mode;
       public IntPtr next_config;
       public IntPtr next_code;
       
       public IntPtr next;
   }
}