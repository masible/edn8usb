using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;

namespace edn8usb
{
    class Program
    {

        static SerialPort port;
        static int map_loaded;

        static void Main(string[] args)
        {
            try
            {

                Console.WriteLine("EverDrive-N8 USB tool v2");
                if (args.Length < 1) throw new Exception("File is not specifed");
                Console.Write("Connect... ");
                connect();
                Console.WriteLine("OK: " + port.PortName);

                map_loaded = 0;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Contains(".rbf") || args[i].Contains(".RBF"))
                    {
                        loadFirm(args[i]);
                        map_loaded = 1;
                        break;
                    }
                }

              

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Contains(".nes") || args[i].Contains(".NES"))
                    {
                        byte mapper = loadRom(args[i]);
                        startGame(mapper);
                        
                    }
                }

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Contains(".fds") || args[i].Contains(".FDS"))
                    {
                        loadFds(args[i]);
                        startGame(254);
                    }
                }
                port.Close();
                //Console.ReadLine();

            }
            catch (Exception x)
            {
                try { port.Close(); }
                catch (Exception) { }
                Console.WriteLine("");
                Console.WriteLine("ERROR: "+x.Message);
                //Console.ReadLine();
            }
        }

       

        static void connect()
        {
            string[] ports = SerialPort.GetPortNames();

            for (int i = 0; i < ports.Length; i++)
            {
                try
                {
                    port = new SerialPort(ports[i]);
                    port.ReadTimeout = 100;
                    port.WriteTimeout = 100;
                    port.Open();
                    port.Write("*t");
                    if (port.ReadByte() == 'k') return;
                }
                catch (Exception) { };

                try
                {
                    port.Close();
                }
                catch (Exception) { }
                port = null;

            }

            if (port == null) throw new Exception("EverDrive is not detected");
        }

        static void loadFirm(String filename)
        {
            Console.Write("Load mapper");
            byte[] buff;
            int len;

            FileStream f = File.OpenRead(filename);
            len = (int)f.Length;
            if (len % 512 != 0) len = len / 512 * 512 + 512;
            buff = new byte[len+1];
            for (int i = 0; i < buff.Length; i++) buff[i] = 0xff;
            f.Read(buff, 1, (int)f.Length);
            f.Close();
            buff[0] = (byte)(len / 512);

            port.ReadTimeout = 1000;
            port.WriteTimeout = 1000;
            port.Write("*s*f");
            txData(buff, 0, buff.Length);
            if (port.ReadByte() != 'k') throw new Exception("Unexpected response");
            Console.WriteLine(" OK");
            System.Console.Write("Run OS...");
            port.ReadTimeout = 6000;
            port.WriteTimeout = 6000;
            port.Write("*r*t");
            if (port.ReadByte() != 'k') throw new Exception("Unexpected response");
            Console.WriteLine(" OK");
        }

        static byte loadRom(String filename)
        {
            
            byte[] prg;
            byte[] chr;
            byte[] hdr = new byte[16];
            byte mapper;
            byte map_cfg;
            FileStream f = File.OpenRead(filename);
            f.Read(hdr, 0, 16);
            if (hdr[0] != 'N' || hdr[1] != 'E' || hdr[2] != 'S') throw new Exception("Bad ROM");
            map_cfg = (byte)(hdr[6] & 0x0b);
            mapper = (byte)((hdr[6] >> 4) | (hdr[7] & 0xf0));
            System.Console.WriteLine("Mapper: " + mapper);
            System.Console.WriteLine("PRG: " + hdr[4]);
            System.Console.WriteLine("CHR: " + hdr[5]);
            prg = new byte[hdr[4] * 16384];
            chr = new byte[hdr[5] * 8192];
            f.Read(prg, 0, prg.Length);
            f.Read(chr, 0, chr.Length);
            f.Close();

            if (map_loaded == 0)
            {
                lpadIntMapper(mapper);
            }

            Console.Write("Load ROM");
            if (prg.Length > 512 * 1024) throw new Exception("PRG size is too big");
            if (chr.Length > 512 * 1024) throw new Exception("CHR size is too big");
            port.ReadTimeout = 2000;
            port.WriteTimeout = 4000;

            port.Write("*g");
            hdr[0] = (byte)(prg.Length / 16384);
            hdr[1] = (byte)(chr.Length / 8192);
            hdr[2] = map_cfg;
            port.Write(hdr, 0, 3);

            txData(prg, 0, prg.Length);
            if (port.ReadByte() != 'k') throw new Exception("Unexpected response");
            txData(chr, 0, chr.Length);
            if (port.ReadByte() != 'k') throw new Exception("Unexpected response");

            Console.WriteLine(" OK");
            return mapper;
        }

        static void startGame(byte mapper)
        {
            byte[] buff = new byte[1];

            System.Console.Write("Run game...");
            port.Write("*r");
            buff[0] = mapper;
            port.Write(buff, 0, 1);
            if (port.ReadByte() != 'k') throw new Exception("Unexpected response");
            System.Console.WriteLine(" OK");
        }

        static void loadFds(String filename)
        {
            byte prg_size;

            if (map_loaded == 0)
            {
                lpadIntMapper(254);
            }
            
            Console.Write("Load FDS");

            byte[] hdr = new byte[16];
            FileStream f = File.OpenRead(filename);
            f.Read(hdr, 0, 16);
            if (hdr[0] != 'F' | hdr[1] != 'D' | hdr[2] != 'S')
            {
                f.Close();
                f = File.OpenRead(filename);
            }
            prg_size = (byte)(f.Length > 65536 ? 8 : 4);
            byte[] buff = new byte[prg_size * 16384];
            f.Read(buff, 0, buff.Length);
            f.Close();

            if (buff[11] != 'H' || buff[12] != 'V' || buff[13] != 'C') throw new Exception("Bad ROM");

            port.Write("*g");
            hdr[0] = (byte)(prg_size);
            hdr[1] = 0;
            hdr[2] = 0;
            port.Write(hdr, 0, 3);

            txData(buff, 0, buff.Length);
            if (port.ReadByte() != 'k') throw new Exception("Unexpected response");
            if (port.ReadByte() != 'k') throw new Exception("Unexpected response");

            Console.WriteLine("OK");
        }

        static void txData(byte[] buff, int offset, int len)
        {
            while(len > 0){
                int sub_len = len < 8192 ? len : 8192;
                port.Write(buff, offset, sub_len);
                len -= sub_len;
                offset += sub_len;
                Console.Write(".");
            }
        }

        static void lpadIntMapper(byte mapper)
        {
            System.Console.Write("Load internal mapper... ");

            port.ReadTimeout = 2000;
            port.WriteTimeout = 4000;

            byte[] cmd = new byte[1];
            cmd[0] = mapper;
            port.Write("*m");
            port.Write(cmd, 0, 1);

            if (port.ReadByte() != 'k') throw new Exception("Unexpected response");
            System.Console.WriteLine(" OK");
        }


    }
}
