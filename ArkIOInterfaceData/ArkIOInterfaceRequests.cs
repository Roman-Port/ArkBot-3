using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace ArkIOInterfaceData
{
    public delegate void ArkIOInterface_Updates(ArkIOInterfaceResponse state, int id);
    public class ArkIOInterfaceRequests
    {
        public static void Cmd_Copy(int id, ArkIOInterfaceRequestData data, ArkIOInterface_Updates callback)
        {
            try
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, false, data.id), id);
                //Copy
                File.Copy(data.strArgOne, data.strArgTwo);
                //Done
                callback(new ArkIOInterfaceResponse(0, 1, 1, true, false, data.id), id);
            } catch
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, true, data.id), id);
            }
        }

        public static void Cmd_Move(int id, ArkIOInterfaceRequestData data, ArkIOInterface_Updates callback)
        {
            try
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, false, data.id), id);
                //Copy
                File.Move(data.strArgOne, data.strArgTwo);
                //Done
                callback(new ArkIOInterfaceResponse(0, 1, 1, true, false, data.id), id);
            }
            catch
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, true, data.id), id);
            }
        }

        public static void Cmd_Delete(int id, ArkIOInterfaceRequestData data, ArkIOInterface_Updates callback)
        {
            try
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, false, data.id), id);
                //Copy
                File.Delete(data.strArgOne);
                //Done
                callback(new ArkIOInterfaceResponse(0, 1, 1, true, false, data.id), id);
            }
            catch
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, true, data.id), id);
            }
        }

        public static void Cmd_DirList(int id, ArkIOInterfaceRequestData data, ArkIOInterface_Updates callback)
        {
            try
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, false, data.id), id);
                string[] files = Directory.GetFiles(data.strArgOne);
                //Done
                var d = new ArkIOInterfaceResponse(0, 1, 1, true, false, data.id);
                d.output = files;
                callback(d, id);
            }
            catch
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, true, data.id), id);
            }
        }

        public static void Cmd_Compress(int id, ArkIOInterfaceRequestData data, ArkIOInterface_Updates callback)
        {
            try
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, false, data.id), id);
                //Copy
                ZipFile.CreateFromDirectory(data.strArgOne, data.strArgTwo);
                //Validate ZIP
                string tmpDir = Path.GetTempPath()+@"RomanPort\";
                if (!Directory.Exists(tmpDir))
                    Directory.CreateDirectory(tmpDir);
                tmpDir += "ArkBot\\";
                if (!Directory.Exists(tmpDir))
                    Directory.CreateDirectory(tmpDir);
                tmpDir += "ZipVerify\\";
                if (!Directory.Exists(tmpDir))
                {
                    Directory.CreateDirectory(tmpDir);
                } else
                {
                    //It exists. Clear it.
                    Directory.Delete(tmpDir, true);
                    Directory.CreateDirectory(tmpDir);
                }
                //Extract here
                ZipFile.ExtractToDirectory(data.strArgTwo, tmpDir);
                //Delete
                Directory.Delete(tmpDir, true);
                //Done
                callback(new ArkIOInterfaceResponse(0, 1, 1, true, false, data.id), id);
            }
            catch
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, true, data.id), id);
            }
        }

        public static void Cmd_StartProcess(int id, ArkIOInterfaceRequestData data, ArkIOInterface_Updates callback)
        {
            try
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, false, data.id), id);

                Process p = Process.Start(data.strArgOne, data.strArgTwo);
                //Done
                var c = new ArkIOInterfaceResponse(0, 1, 1, true, false, data.id);
                c.output = new string[1] { p.Id.ToString() };
                callback(c, id);
            }
            catch
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, true, data.id), id);
            }
        }

        public static void Cmd_StopProcess(int id, ArkIOInterfaceRequestData data, ArkIOInterface_Updates callback)
        {
            try
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, false, data.id), id);

                Process p = Process.GetProcessById(data.intArgOne);
                if(p==null)
                {
                    callback(new ArkIOInterfaceResponse(0, 0, 1, false, true, data.id), id);
                }
                p.CloseMainWindow();
                //Done
                var c = new ArkIOInterfaceResponse(0, 1, 1, true, false, data.id);
                callback(c, id);
            }
            catch
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, true, data.id), id);
            }
        }

        public static void Cmd_GetProcessByName(int id, ArkIOInterfaceRequestData data, ArkIOInterface_Updates callback)
        {
            try
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, false, data.id), id);

                Process[] p = Process.GetProcessesByName(data.strArgOne);
                List<string> s = new List<string>();
                foreach(Process pr in p)
                {
                    s.Add(pr.Id.ToString());
                }
                //Done
                var c = new ArkIOInterfaceResponse(0, 1, 1, true, false, data.id);
                c.output = s.ToArray();
                callback(c, id);
            }
            catch
            {
                callback(new ArkIOInterfaceResponse(0, 0, 1, false, true,data.id), id);
            }
        }
    }
}
