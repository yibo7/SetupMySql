using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using XS.Core;
using XS.Core.FSO;

namespace SetupMySql
{
    public partial class Main : Form
    {
        private string mysqlSetting = @"
[client]                                                                                                      
port=#端口#
default-character-set=utf8
[mysqld]
port=#端口#
character_set_server=utf8
basedir=#数据库程序目录#
datadir=#数据存储目录#
sql_mode=NO_ENGINE_SUBSTITUTION,STRICT_TRANS_TABLES
[WinMySQLAdmin]
#服务路径#

";
        public Main()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterScreen;

            lbPath.Text = $"当前目录:{AppDomain.CurrentDomain.BaseDirectory}";
        }

        private void btnSetup_Click(object sender, EventArgs e)
        {
            

            CmdHelper cmd = new CmdHelper();
            //D:\\web\\BeiMaiProject\\beimai5.0\\Web\\BeiMai.WebApp\\BeiMai.WebApp\\bin
            string sPath = AppDomain.CurrentDomain.BaseDirectory;
            //SC Delete MongoDB  --删除服务 
            string sPan = Path.GetPathRoot(sPath);

            string sPassWord = txtPass.Text.Trim();
            if (string.IsNullOrEmpty(sPassWord))
            {
                MessageBox.Show("请输入正确的密码！");
                return;
            }

            string sPort = txtPort.Text.Trim();

            if (string.IsNullOrEmpty(sPort) || sPort.Length < 4 || !XS.Core.XsUtils.IsNumeric(sPort))
            {
                MessageBox.Show("请输入正确的端口,端口为4位到5位的数字！");
                return;
            }

            string basedir = string.Concat(sPath, "\\db");


            string datadir = string.Concat(sPath, "\\db\\data");

            string WinMySQLAdminPath = string.Concat(sPath, "\\db\\bin\\mysqld.exe");

            

            if (!FObject.IsExist(basedir, FsoMethod.Folder)|| !FObject.IsExist(WinMySQLAdminPath, FsoMethod.File))
            {
                MessageBox.Show("当前目录下不存在数据库源程序！");
                return;
            }
            btnSetup.Enabled = false;
            btnSetup.Text = "开始安装...";
            if (cmd.CheckService("MySQL"))
            {
                bool isok = WinformTools.ConfirmDialog("已经安装了MYSQL,是否删除，确认将删除,结束后再重试安装！");
                if (isok)
                {
                    btnSetup.Text = "正在删除旧服务...";
                    Thread threadCpuInfo2 = new Thread(() =>
                    {

                        cmd.RunCmd("net stop MySQL");
                        Thread.Sleep(30000);
                        cmd.RunCmd("SC Delete MySQL");
                        Thread.Sleep(3000);
                        Action<int> action = (data) =>
                        {
                            btnSetup.Enabled = true;
                            btnSetup.Text = "原服务已经删除，请点击重试安装";
                        };
                        Invoke(action, 1);

                    });

                    threadCpuInfo2.Start();

                }
                else
                {
                    btnSetup.Enabled = true;
                    btnSetup.Text = "开始安装";
                }

                return;

            }

            if(FObject.IsExist(datadir, FsoMethod.Folder))
            {
                bool isok = WinformTools.ConfirmDialog("当前程序下已经存在数据库数据，是否要先删除！");
                if (isok)
                {
                    string dataPath = string.Concat(sPath, "\\db\\data");
                    if (FObject.IsExist(dataPath, FsoMethod.Folder))
                    {
                        FObject.Delete(dataPath, FsoMethod.Folder);
                        btnSetup.Enabled = true;
                        btnSetup.Text = "旧数据已经删除，请点击重试安装";
                    }
                }
                else
                {
                    btnSetup.Enabled = true;
                    btnSetup.Text = "开始安装";
                }

                return;
            }


            mysqlSetting = mysqlSetting.Replace("#端口#", sPort).Replace("#数据库程序目录#", basedir).Replace("#数据存储目录#", datadir).Replace("#服务路径#",WinMySQLAdminPath);

            string dbSettingFile = string.Concat(sPath, "\\db\\bin\\my.ini");

            FObject.WriteFileUtf8(dbSettingFile, mysqlSetting);

             

            Thread threadCpuInfo = new Thread(() =>
            {

                 
                List<string> cmds = new List<string>();
                cmds.Add(string.Format("cd {0}", sPan));
                cmds.Add(string.Format(@"cd {0}db\bin", sPath));
                cmds.Add(@"mysqld --initialize-insecure");
                //cmds.Add(@"mysqld --install");
                //cmds.Add(@"net start mysql");
                //cmds.Add(@"mysqladmin -u root password");
                //cmds.Add(@"mysql -uroot -p");
                //cmds.Add(sPassWord);
                //cmds.Add(sPassWord);
                cmd.RunCmd(cmds);
                Thread.Sleep(50000);
                cmds.Clear();
                cmds.Add(string.Format("cd {0}", sPan));
                cmds.Add(string.Format(@"cd {0}db\bin", sPath));
                cmds.Add(@"mysqld --install");
                cmd.RunCmd(cmds);
                Thread.Sleep(30000);
                cmds.Clear();
                cmds.Add(string.Format("cd {0}", sPan));
                cmds.Add(string.Format(@"cd {0}db\bin", sPath));
                cmds.Add(@"net start mysql");
                cmd.RunCmd(cmds);
                Thread.Sleep(30000);
                cmds.Clear();
                cmds.Add(string.Format("cd {0}", sPan));
                cmds.Add(string.Format(@"cd {0}db\bin", sPath));
                cmds.Add(string.Format(@"mysqladmin -u root password {0}", sPassWord));
                //cmds.Add(sPassWord);
                //cmds.Add(sPassWord);
                cmd.RunCmd(cmds);

                Thread.Sleep(30000);
                Action<int> action = (data) =>
                {
                    btnSetup.Enabled = true;
                    btnSetup.Text = "开始安装";
                };
                Invoke(action,1);
            
                
            });


            threadCpuInfo.Start();

        }

        private void btnSetPrivateKey_Click(object sender, EventArgs e)
        {
            string Key = "coin2018";
            try
            {
                var oldPwd = txtPass.Text.Trim();
                if (!string.IsNullOrEmpty(oldPwd))
                {
                    //var enPwd = GetEncrypt(oldPwd);
                    var enPwd = XS.Core.XsUtils.MD5(string.Concat(oldPwd, Key));
                    SetConfigValue("Password", enPwd);

                    MessageBox.Show("加密完成");
                    //var testPwd = GetDecrypt(enPwd);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        /// <summary>
        /// 修改AppSettings中配置
        /// </summary>
        /// <param name="key">key值</param>
        /// <param name="value">相应值</param>
        private bool SetConfigValue(string key, string value)
        {
            try
            {
                var map = new ExeConfigurationFileMap();
                map.ExeConfigFilename = @"ColdWallet.exe.Config";

                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[key] != null)
                    config.AppSettings.Settings[key].Value = value;
                else
                    config.AppSettings.Settings.Add(key, value);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
