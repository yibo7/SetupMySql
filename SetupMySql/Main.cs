using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
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
        private string mysqlSetting = @"[client]                                                                                                      
port=#端口#
default-character-set=utf8
[mysqld]
port=#端口#
character_set_server=utf8
";
        public Main()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;//使最大化窗口失效
            //下一句用来禁止对窗口大小进行拖拽
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;

            lbPath.Text = $"当前目录:{AppDomain.CurrentDomain.BaseDirectory}";
        }

        private void btnSetup_Click(object sender, EventArgs e)
        { 
            pbSetup.Maximum = 100; 
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

            if (string.IsNullOrEmpty(sPort) || sPort.Length < 4 || !Tools.IsNumeric(sPort))
            {
                MessageBox.Show("请输入正确的端口,端口为4位到5位的数字！");
                return;
            }

            bool paortUsed = Tools.portInUse(Tools.StrToInt(sPort));

            if (paortUsed)
            {
                MessageBox.Show($"端口{sPort}已经其他程序占用！");
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



            if (Tools.CheckService("MySQL"))
            {
                bool isok = Tools.ConfirmDialog("已经安装了MYSQL,是否删除，确认将删除,结束后再重试安装！");
                if (isok)
                {
                    btnSetup.Text = "正在删除旧服务...";
                    Thread threadCpuInfo2 = new Thread(() =>
                    {

                       bool isRuned = Tools.RunCmd("net stop MySQL");
                        if (isRuned)
                        {
                            isRuned = Tools.RunCmd("SC Delete MySQL");
                            if (isRuned)
                            {
                                Action<int> action = (data) =>
                                {
                                    btnSetup.Enabled = true;
                                    btnSetup.Text = "原服务已经删除，请点击重试安装";
                                };
                                Invoke(action, 1);
                            }
                            else
                            {
                                MessageBox.Show("旧服务删除失败");
                            }
                           

                        }
                            

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
                bool isok = Tools.ConfirmDialog("当前程序下已经存在数据库数据，是否要先删除！");
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

            string dbSettingFile = string.Concat(sPath, "\\db\\my.ini");

            FObject.WriteFile(dbSettingFile, mysqlSetting);


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
                bool isruned = Tools.RunCmd(cmds);
                if (isruned)
                {
                    Action<int> proce_update = (data) =>
                    {
                        pbSetup.Value = data;
                    };
                    Invoke(proce_update, 20);
                    
                    //Thread.Sleep(50000);
                    cmds.Clear();
                    cmds.Add(string.Format("cd {0}", sPan));
                    cmds.Add(string.Format(@"cd {0}db\bin", sPath));
                    cmds.Add(@"mysqld --install");
                    isruned = Tools.RunCmd(cmds);
                    if (isruned)
                    {
                        Invoke(proce_update, 50); 
                        cmds.Clear();
                        cmds.Add(string.Format("cd {0}", sPan));
                        cmds.Add(string.Format(@"cd {0}db\bin", sPath));
                        cmds.Add(@"net start mysql");
                        isruned = Tools.RunCmd(cmds);
                        if (isruned)
                        {
                            Invoke(proce_update, 60);
                            cmds.Clear();
                            cmds.Add(string.Format("cd {0}", sPan));
                            cmds.Add(string.Format(@"cd {0}db\bin", sPath));
                            cmds.Add(string.Format(@"mysqladmin -u root password {0}", sPassWord));
                            //cmds.Add(sPassWord);
                            //cmds.Add(sPassWord);
                            isruned = Tools.RunCmd(cmds);
                            if(isruned)
                            {
                                Thread.Sleep(2000);
                                Invoke(proce_update, 70);                               
                                
                                //配置 root 用户远程访问-mysql 8.0 后需要执行以下命令
                                cmds.Clear();
                                cmds.Add(string.Format("cd {0}", sPan));
                                cmds.Add(string.Format(@"cd {0}db\bin", sPath));
                                // 创建 root@'%' 用户并设置密码
                                cmds.Add(string.Format(@"mysql -u root -p{0} --port={1} -e ""CREATE USER 'root'@'%' IDENTIFIED WITH mysql_native_password BY '{0}';""", sPassWord, sPort));
                                
                                Thread.Sleep(1000);
                                Invoke(proce_update, 80);
                                
                                // 授予所有权限
                                cmds.Add(string.Format(@"mysql -u root -p{0} --port={1} -e ""GRANT ALL PRIVILEGES ON *.* TO 'root'@'%' WITH GRANT OPTION;""", sPassWord, sPort));
                                
                                Thread.Sleep(1000);
                                Invoke(proce_update, 90);
                                
                                // 确保使用 mysql_native_password（可选，CREATE USER 已设置）
                                cmds.Add(string.Format(@"mysql -u root -p{0} --port={1} -e ""ALTER USER 'root'@'%' IDENTIFIED WITH mysql_native_password BY '{0}';""", sPassWord, sPort));
                                isruned = Tools.RunCmd(cmds);

                                if (isruned)
                                {
                                    Invoke(proce_update, 100);
                                    MessageBox.Show("安装完成！");
                                    Action<int> action = (data) =>
                                    {
                                        btnSetup.Enabled = true;
                                        btnSetup.Text = "开始安装";
                                    };
                                    Invoke(action, 1);
                                }
                                else
                                {
                                    MessageBox.Show("配置 root 远程访问失败。");
                                }

                                //Invoke(proce_update, 100);
                                //MessageBox.Show("安装完成!");
                                //Action<int> action = (data) =>
                                //{
                                //    btnSetup.Enabled = true;
                                //    btnSetup.Text = "开始安装";
                                //};
                                //Invoke(action, 1);
                            }
                            else
                            {
                                MessageBox.Show("修改密码失败！");
                            }
                        }
                    }
                } 
                
            
                
            });


            threadCpuInfo.Start();

        } 
        
        

        private void lb_del_mysql_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            bool isok = Tools.ConfirmDialog("确定要删除MYSQL的服务吗！");
            if (isok)
            {
                lb_del_mysql.Enabled = false;
                Thread threadCpuInfo2 = new Thread(() =>
                {

                    bool isRuned = Tools.RunCmd("net stop MySQL");
                    if (isRuned)
                    {
                        isRuned = Tools.RunCmd("SC Delete MySQL");
                        if (isRuned)
                        {
                            Action<int> action = (data) =>
                            {
                                lb_del_mysql.Enabled = true;
                                lb_del_mysql.Text = "MYSQL已经删除";
                            };
                            Invoke(action, 1);
                        }
                        else
                        {
                            MessageBox.Show("旧服务删除失败");
                        }


                    }


                });

                threadCpuInfo2.Start();
            }
            
        }
    }
}
