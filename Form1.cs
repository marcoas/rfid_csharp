using Newtonsoft.Json;
using ReaderB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace ZK_RFID102demomain
{
    public partial class Form1 : Form
    {
        private bool fAppClosed; // Cierre responsivo de la aplicación en modo de prueba
        private byte fComAdr=0xff; // ComAdr actualmente en funcionamiento
        private int ferrorcode;
        private byte fBaud;
        private double fdminfre;
        private double fdmaxfre;
        private byte Maskadr;
        private byte MaskLen;
        private byte MaskFlag;
        private int fCmdRet=30; // Valores de retorno de todas las instrucciones ejecutadas
        private int fOpenComIndex; // Número de índice del puerto serie abierto
        private bool fIsInventoryScan;
        private byte[] fOperEPC=new byte[36];
        private byte[] fPassWord=new byte[4];
        private byte[] fOperID_6B=new byte[8];
        ArrayList list = new ArrayList();
        private string fInventory_EPC_List; // Almacenar lista de consultas (si los datos leídos no han cambiado, no se realiza ninguna actualización)
        private int frmcomportindex;
        private bool ComOpen= false;
        public Form1()
        {
            InitializeComponent();

            // Marco.
            // Fuerza el uso de TLS
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

        }


        // Marco
        private RFIDCollector lector = new RFIDCollector();


        public void EnviarPost(string sEPC)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        // Fuerza el uso de TLS
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

                        client.Headers[HttpRequestHeader.ContentType] = "application/json";

                        var datos = new
                        {
                            user = "marcoAS",
                            pass = "1234",
                            tagID = sEPC
                        };
                        // string json = "{\"user\":\"marcoAS\",\"pass\":\"1234\", \"tagID\": \"" + sEPC + "\"}";
                        string json = JsonConvert.SerializeObject(datos);

                        string respuesta = client.UploadString("https://mpb36649e50544b887b2.free.beeceptor.com", "POST", json);
                        // string respuesta = client.UploadString("http://marco.dpsonline.com.ar/gargano", "GET");

                        MessageBox.Show(respuesta);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERROR: " + ex.Message);
                }
            });
        }

        private string GetReturnCodeDesc(int cmdRet)
        {
            switch (cmdRet)
            {
                case 0x00:
                    return "Exito";
                case 0x01:
                    return "Devuelva antes de que finalice el tiempo de consulta";
                case 0x02:
                    return "El tiempo de consulta especificado ha expirado";
                case 0x03:
                    return "Después de este mensaje, hay más noticias.";
                case 0x04:
                    return "Esapcio lleno";
                case 0x05:
                    return "Error de contraseña de acceso";
                case 0x09:
                    return "Error de destrucción de contraseña";
                case 0x0a:
                    return "La contraseña de destrucción no puede ser solo 0";
                case 0x0b:
                    return "La etiqueta electrónica no admite este comando.";
                case 0x0c:
                    return "Para este comando, la contraseña de acceso no puede ser toda 0";
                case 0x0d:
                    return "La etiqueta electrónica se ha configurado para protección de lectura y no se puede configurar nuevamente";
                case 0x0e:
                    return "La etiqueta electrónica no está protegida contra lectura y no es necesario desbloquearla.";
                case 0x10:
                    return "Hay espacio de bytes bloqueado, la escritura falló";
                case 0x11:
                    return "No se puede bloquear";
                case 0x12:
                    return "Ya bloqueado, no se puede volver a bloquear";
                case 0x13:
                    return "Error al guardar el parámetro, pero el valor establecido es válido antes de que se apague el módulo de lectura/escritura";
                case 0x14:
                    return "No se puede ajustar";
                case 0x15:
                    return "Devuelva antes de que finalice el tiempo de consulta";
                case 0x16:
                    return "El tiempo de consulta especificado ha expirado";
                case 0x17:
                    return "Después de este mensaje, hay más noticias.";
                case 0x18:
                    return "El espacio de almacenamiento del módulo de lectura/escritura está lleno";
                case 0x19:
                    return "El dispositivo electrónico no admite este comando o la contraseña de acceso no puede ser 0";
                case 0xFA:
                    return "Hay una etiqueta electrónica, pero la comunicación es deficiente y no se puede operar.";
                case 0xFB:
                    return "No se pueden utilizar etiquetas electrónicas";
                case 0xFC:
                    return "La etiqueta electrónica devuelve un código de error";
                case 0xFD:
                    return "Error de longitud del comando";
                case 0xFE:
                    return "Comando ilegal";
                case 0xFF:
                    return "Error de parámetro";
                case 0x30:
                    return "Error de comunicación";
                case 0x31:
                    return "Error de comprobación de CRC";
                case 0x32:
                    return "La longitud de los datos devueltos es incorrecta";
                case 0x33:
                    return "La comunicación está ocupada y el dispositivo está ejecutando otras instrucciones";
                case 0x34:
                    return "Ocupado, el comando se está ejecutando";
                case 0x35:
                    return "El puerto está abierto";
                case 0x36:
                    return "El puerto está abierto";
                case 0x37: 
                    return "Identificador no válido";
                case 0x38:
                    return "Puerto no válido";
                case 0xEE:
                    return "Error de comando de retorno";
                default:
                    return "";
            }
        }
        private string GetErrorCodeDesc(int cmdRet)
        {
            switch (cmdRet)
            {
                case 0x00:
                    return "Otros errores";
                case 0x03:
                    return "Límite de memoria excedido o valor de PC no compatible";
                case 0x04:
                    return "Bloqueo de memoria";
                case 0x0b:
                    return "Suministro de energía insuficiente";
                case 0x0f:
                    return "Error no específico";
                default:
                    return "";
            }
        }
        private byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }

        private string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
            return sb.ToString().ToUpper();

        }
        private void AddCmdLog(string CMD, string cmdStr, int cmdRet)
        {
            try
            {
                StatusBar1.Panels[0].Text = "";
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + " " +
                                            cmdStr + ": " +
                                            GetReturnCodeDesc(cmdRet);
            }
            finally
            {
                ;
            }
        }
        private void AddCmdLog(string CMD, string cmdStr, int cmdRet,int errocode)
        {
            try
            {
                StatusBar1.Panels[0].Text = "";
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + " " +
                                            cmdStr + ": " +
                                            GetReturnCodeDesc(cmdRet)+" "+"0x"+Convert.ToString(errocode,16).PadLeft(2,'0');
            }
            finally
            {
                ;
            }
        }
        private void ClearLastInfo()
        { 
            

              Edit_Type.Text = "";
              Edit_Version.Text = "";
              ISO180006B.Checked=false;
              EPCC1G2.Checked=false;
              Edit_ComAdr.Text = "";
              Edit_powerdBm.Text = "";
              Edit_scantime.Text = "";
              Edit_dminfre.Text = "";
              Edit_dmaxfre.Text = "";
            //  PageControl1.TabIndex = 0;
        }
        private void InitReaderList()
        {
            int i=0;
           // ComboBox_PowerDbm.SelectedIndex = 0;
            ComboBox_baud.SelectedIndex =3;
             for (i=0 ;i< 63;i++)
             {
                ComboBox_dminfre.Items.Add(Convert.ToString(902.6+i*0.4)+" MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(902.6 + i * 0.4) + " MHz");
             }
             ComboBox_dmaxfre.SelectedIndex = 62;
              ComboBox_dminfre.SelectedIndex = 0;
              for (i=0x03;i<=0xff;i++)
                  ComboBox_scantime.Items.Add(Convert.ToString(i) + "*100ms");
              ComboBox_scantime.SelectedIndex = 7;
              i=40;
              while (i<=300)
              {
                  ComboBox_IntervalTime.Items.Add(Convert.ToString(i) + "ms");
              i=i+10;
              }
              ComboBox_IntervalTime.SelectedIndex = 1;

              for (i = 0; i < 256; i++)
              {
                  comboBox6.Items.Add(Convert.ToString(i) + "*1s");
              }
              comboBox6.SelectedIndex = 0;
              for (i = 1; i < 33; i++)
              {
                  comboBox5.Items.Add(Convert.ToString(i));
              }
              comboBox5.SelectedIndex = 0;
              comboBox4.SelectedIndex = 0;
              ComboBox_PowerDbm.SelectedIndex = 30;
              comboBox7.SelectedIndex = 8;
              for (i = 0; i < 101; i++)
              {
                  comboBox_OffsetTime.Items.Add(Convert.ToString(i) + "*1ms");
              }
              comboBox_OffsetTime.SelectedIndex = 5;

             for (i=0;i< 255;i++)
              comboBox_tigtime.Items.Add(Convert.ToString(i)+"*1s");
              comboBox_tigtime .SelectedIndex= 0;   //
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            progressBar1.Visible = false;
            fOpenComIndex = -1;
            fComAdr = 0;
            ferrorcode = -1;
            fBaud = 5;
            InitReaderList();

            C_EPC.Checked = true;
            fAppClosed = false;
            fIsInventoryScan = false;
            Timer_Test_.Enabled = false;
            Timer_G2_Read.Enabled = false;
            Timer_G2_Alarm.Enabled = false;
            timer1.Enabled = false;

            Button3.Enabled = false;
            Button5.Enabled = false;
            Button1.Enabled = false;
            button2.Enabled = false;
            SpeedButton_Read_G2.Enabled = false;
            Button_DataWrite.Enabled = false;
            BlockWrite.Enabled = false;
            Button_BlockErase.Enabled = false;

            radioButton5.Checked = true;
            radioButton7.Checked = true;
            radioButton10.Checked = true;
            radioButton14.Checked = true;
            button8.Enabled = false;
            button9.Enabled = false;
            comboBox5.Enabled = false;
            radioButton5.Enabled = false;
            radioButton6.Enabled = false;
            radioButton7.Enabled = false;
            radioButton8.Enabled = false;
            radioButton9.Enabled = false;
            radioButton10.Enabled = false;
            radioButton11.Enabled = false;
            radioButton12.Enabled = false;
            radioButton13.Enabled = false;
            radioButton14.Enabled = false;
            radioButton15.Enabled = false;
            textBox3.Enabled = false;
            radioButton_band1.Checked = true;
            radioButton16.Enabled = false;
            radioButton17.Enabled = false;
            radioButton18.Enabled = false;
            radioButton19.Enabled = false;
            radioButton16.Checked = true;
            radioButton21.Checked = true;

            // Marco. Abrir Puerto
            OpenNetPort_Click(null, EventArgs.Empty);
        }


        private void Button3_Click(object sender, EventArgs e)
        {
             byte[] TrType=new byte[2];
             byte[] VersionInfo=new byte[2];
             byte ReaderType=0;
             byte ScanTime=0;
             byte dmaxfre=0;
             byte dminfre = 0;
             byte powerdBm=0;
             byte FreBand = 0;
             Edit_Version.Text = "";
              Edit_ComAdr.Text = "";
              Edit_scantime.Text = "";
              Edit_Type.Text = "";
              ISO180006B.Checked=false;
              EPCC1G2.Checked=false;
              Edit_powerdBm.Text = "";
              Edit_dminfre.Text = "";
              Edit_dmaxfre.Text = "";
              ComboBox_PowerDbm.Items.Clear();
              fCmdRet = StaticClassReaderB.GetReaderInformation(ref fComAdr, VersionInfo, ref ReaderType, TrType, ref dmaxfre, ref dminfre, ref powerdBm, ref ScanTime, frmcomportindex);
              if (fCmdRet == 0)
              {
                  Edit_Version.Text = Convert.ToString(VersionInfo[0], 10).PadLeft(2, '0') + "." + Convert.ToString(VersionInfo[1], 10).PadLeft(2, '0');
                  if (VersionInfo[1]>= 30)
                  {
                      for (int i=0;i< 31;i++ )
                      ComboBox_PowerDbm.Items.Add(Convert.ToString(i));
                      if(powerdBm>30)
                        ComboBox_PowerDbm.SelectedIndex=30;
                      else
                      ComboBox_PowerDbm.SelectedIndex=powerdBm;
                  }
                  else
                  {
                      for (int i=0;i< 19;i++ )
                        ComboBox_PowerDbm.Items.Add(Convert.ToString(i));
                    if (powerdBm > 18)
                        ComboBox_PowerDbm.SelectedIndex = 18;
                    else
                        ComboBox_PowerDbm.SelectedIndex = powerdBm;
                  }
                  Edit_ComAdr.Text = Convert.ToString(fComAdr, 16).PadLeft(2, '0');
                  Edit_NewComAdr.Text = Convert.ToString(fComAdr, 16).PadLeft(2, '0');
                  Edit_scantime.Text = Convert.ToString(ScanTime, 10).PadLeft(2, '0') + "*100ms";
                  ComboBox_scantime.SelectedIndex = ScanTime - 3;
                  Edit_powerdBm.Text = Convert.ToString(powerdBm, 10).PadLeft(2, '0');

                  FreBand= Convert.ToByte(((dmaxfre & 0xc0)>> 4)|(dminfre >> 6)) ;
                  switch (FreBand)
                  {
                      case 0:
                          {
                              radioButton_band1.Checked = true;
                              fdminfre = 902.6 + (dminfre & 0x3F) * 0.4;
                              fdmaxfre = 902.6 + (dmaxfre & 0x3F) * 0.4;
                          }
                          break;
                      case 1:
                          {
                              radioButton_band2.Checked = true;
                              fdminfre = 920.125 + (dminfre & 0x3F) * 0.25;
                              fdmaxfre = 920.125 + (dmaxfre & 0x3F) * 0.25;
                          }
                          break;
                      case 2:
                          {
                              radioButton_band3.Checked = true;
                              fdminfre = 902.75 + (dminfre & 0x3F) * 0.5;
                              fdmaxfre = 902.75 + (dmaxfre & 0x3F) * 0.5;
                          }
                          break;
                      case 3:
                          {
                              radioButton_band4.Checked = true;
                              fdminfre = 917.1 + (dminfre & 0x3F) * 0.2;
                              fdmaxfre = 917.1 + (dmaxfre & 0x3F) * 0.2;
                          }
                          break;
                      case 4:
                          {
                              radioButton_band5.Checked = true;
                              fdminfre = 865.1 + (dminfre & 0x3F) * 0.2;
                              fdmaxfre = 865.1 + (dmaxfre & 0x3F) * 0.2;
                          }
                          break;
                  }
                  Edit_dminfre.Text = Convert.ToString(fdminfre) + "MHz";
                  Edit_dmaxfre.Text = Convert.ToString(fdmaxfre) + "MHz";
                  if (fdmaxfre != fdminfre)
                      CheckBox_SameFre.Checked = false;
                  ComboBox_dminfre.SelectedIndex = dminfre & 0x3F;
                  ComboBox_dmaxfre.SelectedIndex = dmaxfre & 0x3F;
                  if (ReaderType == 0x03)
                      Edit_Type.Text = "";
                  if (ReaderType == 0x06)
                      Edit_Type.Text = "";
                  if (ReaderType == 0x09)
                      Edit_Type.Text = "ZK_RFID101/2";
                  if ((TrType[0] & 0x02) == 0x02) //第二个字节低第四位代表支持的协议“ISO/IEC 15693”
                  {
                      ISO180006B.Checked = true;
                      EPCC1G2.Checked = true;
                  }
                  else
                  {
                      ISO180006B.Checked = false;
                      EPCC1G2.Checked = false;
                  }
              }
              AddCmdLog("GetReaderInformation", "Obtener información del lector", fCmdRet);
        }

        private void Button5_Click(object sender, EventArgs e)
        {
              byte aNewComAdr, powerDbm, dminfre, dmaxfre, scantime, band=0;
              string returninfo="";
              string returninfoDlg="";
              string setinfo;
              if (radioButton_band1.Checked)
                  band = 0;
              if (radioButton_band2.Checked)
                  band = 1;
              if (radioButton_band3.Checked)
                  band = 2;
              if (radioButton_band4.Checked)
                  band = 3;
              if (radioButton_band5.Checked)
                  band = 4;
              if (Edit_NewComAdr.Text == "")
                  return;
              progressBar1.Visible = true;
              progressBar1.Minimum = 0;
              dminfre = Convert.ToByte(((band & 3) << 6) | (ComboBox_dminfre.SelectedIndex & 0x3F));
              dmaxfre = Convert.ToByte(((band & 0x0c) << 4) | (ComboBox_dmaxfre.SelectedIndex & 0x3F));
                  aNewComAdr = Convert.ToByte(Edit_NewComAdr.Text);
                  powerDbm = Convert.ToByte(ComboBox_PowerDbm.SelectedIndex);
                  fBaud = Convert.ToByte(ComboBox_baud.SelectedIndex);
                  if (fBaud > 2)
                      fBaud = Convert.ToByte(fBaud + 2);
                  scantime = Convert.ToByte(ComboBox_scantime.SelectedIndex + 3);
                  setinfo = "Grabar";
              progressBar1.Value =10;     
              fCmdRet = StaticClassReaderB.WriteComAdr(ref fComAdr,ref aNewComAdr,frmcomportindex);
              if (fCmdRet==0x13)
              fComAdr = aNewComAdr;
              if (fCmdRet == 0)
              {
                fComAdr = aNewComAdr;
                returninfo = returninfo + setinfo + "Dirección del lector correcta";
              }
              else if (fCmdRet==0xEE )
                  returninfo = returninfo + setinfo + "Error en el comando de retorno de dirección del lector/escritor";
              else
              {
                  returninfo = returninfo + setinfo + "Error en la dirección del lector";
                  returninfoDlg = returninfoDlg + setinfo + "Comando de retorno de error de dirección del lector=0x"
                   + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
              }
              progressBar1.Value =25; 
              fCmdRet = StaticClassReaderB.SetPowerDbm(ref fComAdr,powerDbm,frmcomportindex);
              if (fCmdRet == 0)
                  returninfo = returninfo + ",Éxito de poder";
              else if (fCmdRet==0xEE )
                  returninfo = returninfo + ",Error en el comando de retorno de energía";
              else
              {
                  returninfo = returninfo + ",Fallo de energía";
                  returninfoDlg = returninfoDlg + " " + setinfo + "Comando de retorno de falla de energía=0x"
                       + Convert.ToString(fCmdRet)+"("+GetReturnCodeDesc(fCmdRet)+")";
              }
              
              progressBar1.Value =40; 
              fCmdRet = StaticClassReaderB.Writedfre(ref fComAdr,ref dmaxfre,ref dminfre,frmcomportindex);
              if (fCmdRet == 0 )
                  returninfo = returninfo + ",Éxito de frecuencia";
              else if (fCmdRet==0xEE)
                  returninfo = returninfo + ",Error en el comando de retorno de frecuencia";
              else
              {
                  returninfo = returninfo + ",Fallo de frecuencia";
                  returninfoDlg = returninfoDlg + " " + setinfo + "Retorno del comando de falla de frecuencia=0x"
                   + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
              }

                    progressBar1.Value =55; 
                  fCmdRet = StaticClassReaderB.Writebaud(ref fComAdr,ref fBaud,frmcomportindex);
                  if (fCmdRet == 0)
                      returninfo = returninfo + ",Éxito en la tasa de baudios";
                  else if (fCmdRet==0xEE)
                      returninfo = returninfo + ",Error en el comando de retorno de velocidad en baudios";
                  else
                  {
                      returninfo = returninfo + ",Fallo de velocidad en baudios";
                      returninfoDlg = returninfoDlg + " " + setinfo + "El comando de falla de velocidad en baudios regresa=0x"
                       + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
                  }

             progressBar1.Value =70; 
              fCmdRet = StaticClassReaderB.WriteScanTime(ref fComAdr,ref scantime,frmcomportindex);
              if (fCmdRet == 0 )
                  returninfo = returninfo + ",Tiempo de consulta exitoso";
             else if (fCmdRet==0xEE)
                  returninfo = returninfo + ",Error en el comando de retorno en tiempo de consulta";
              else
              {
                  returninfo = returninfo + ",Error en el tiempo de consulta";
                  returninfoDlg = returninfoDlg + " " + setinfo + "El tiempo de consulta falla y el comando devuelve = 0x"
                   + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
             }

              progressBar1.Value =100; 
              Button3_Click(sender,e);
              progressBar1.Visible=false;
              StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + returninfo;
              if  (returninfoDlg!="")
                  MessageBox.Show(returninfoDlg, "pista");


        }

        private void Button1_Click(object sender, EventArgs e)
        {
            byte aNewComAdr, powerDbm, dminfre, dmaxfre, scantime;
            string returninfo = "";
            string returninfoDlg = "";
            string setinfo;
            progressBar1.Visible = true;
            progressBar1.Minimum = 0;
             dminfre = 0;
            dmaxfre = 62;
            aNewComAdr =0x00;
            if (Convert.ToInt32(Edit_Version.Text.Substring(3, 2)) >= 30)
                powerDbm = 30;
            else
                powerDbm=18;
            fBaud=5;
            scantime=10;
            setinfo=" 恢复 ";
            ComboBox_baud.SelectedIndex = 3;
            progressBar1.Value = 10;
            fCmdRet = StaticClassReaderB.WriteComAdr(ref fComAdr, ref aNewComAdr, frmcomportindex);
            if (fCmdRet == 0x13)
                fComAdr = aNewComAdr;
            if (fCmdRet == 0)
            {
                fComAdr = aNewComAdr;
                returninfo = returninfo + setinfo + "Dirección del lector correcta";
            }
            else if (fCmdRet == 0xEE)
                returninfo = returninfo + setinfo + "Error en el comando de retorno de dirección del lector/escritor";
            else
            {
                returninfo = returninfo + setinfo + "Error en la dirección del lector";
                returninfoDlg = returninfoDlg + setinfo + "Comando de retorno de error de dirección del lector=0x"
                 + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
            }

            progressBar1.Value = 25;
            fCmdRet = StaticClassReaderB.SetPowerDbm(ref fComAdr, powerDbm, frmcomportindex);
            if (fCmdRet == 0)
                returninfo = returninfo + ",Éxito de poder";
            else if (fCmdRet == 0xEE)
                returninfo = returninfo + ",Error en el comando de retorno de energía";
            else
            {
                returninfo = returninfo + ",Fallo de energía";
                returninfoDlg = returninfoDlg + " " + setinfo + "Comando de retorno de falla de energía=0x"
                     + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
            }

            progressBar1.Value = 40;
            fCmdRet = StaticClassReaderB.Writedfre(ref fComAdr, ref dmaxfre, ref dminfre, frmcomportindex);
            if (fCmdRet == 0)
                returninfo = returninfo + ",Éxito de frecuencia";
            else if (fCmdRet == 0xEE)
                returninfo = returninfo + ",Error en el comando de retorno de frecuencia";
            else
            {
                returninfo = returninfo + ",Fallo de frecuencia";
                returninfoDlg = returninfoDlg + " " + setinfo + "Retorno del comando de falla de frecuencia=0x"
                 + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
            }


            progressBar1.Value = 55;
            fCmdRet = StaticClassReaderB.Writebaud(ref fComAdr, ref fBaud, frmcomportindex);
            if (fCmdRet == 0)
                returninfo = returninfo + ",Éxito en la tasa de baudios";
            else if (fCmdRet == 0xEE)
                returninfo = returninfo + ",Error en el comando de retorno de velocidad en baudios";
            else
            {
                returninfo = returninfo + ",Fallo de velocidad en baudios";
                returninfoDlg = returninfoDlg + " " + setinfo + "El comando de falla de velocidad en baudios regresa=0x"
                 + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
            }

            progressBar1.Value = 70;
            fCmdRet = StaticClassReaderB.WriteScanTime(ref fComAdr, ref scantime, frmcomportindex);
            if (fCmdRet == 0)
                returninfo = returninfo + ",Tiempo de consulta exitoso";
            else if (fCmdRet == 0xEE)
                returninfo = returninfo + ",Error en el comando de retorno en tiempo de consulta";
            else
            {
                returninfo = returninfo + ",Error en el tiempo de consulta";
                returninfoDlg = returninfoDlg + " " + setinfo + "El comando de tiempo de consulta fallido regresa=0x"
                 + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
            }

            progressBar1.Value = 100;
            Button3_Click(sender, e);
            progressBar1.Visible = false;
            StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + returninfo;
            if (returninfoDlg != "")
                MessageBox.Show(returninfoDlg, "pista");
            
        }

        private void CheckBox_SameFre_CheckedChanged(object sender, EventArgs e)
        {
             if (CheckBox_SameFre.Checked)
              ComboBox_dmaxfre.SelectedIndex = ComboBox_dminfre.SelectedIndex;
        }


        private void ComboBox_dfreSelect(object sender, EventArgs e)
        {
             if (CheckBox_SameFre.Checked )
             {
                ComboBox_dminfre.SelectedIndex =ComboBox_dmaxfre.SelectedIndex;
             }
              else if  (ComboBox_dminfre.SelectedIndex> ComboBox_dmaxfre.SelectedIndex )
             {
                 ComboBox_dminfre.SelectedIndex = ComboBox_dmaxfre.SelectedIndex;
                 MessageBox.Show("La frecuencia mínima debe ser menor o igual a la frecuencia máxima", "Mensaje de error");
              }
        }
        public void ChangeSubItem(ListViewItem ListItem, int subItemIndex, string ItemText)
        {
            if (subItemIndex == 1)
            {
                if (ItemText=="")
                {
                    ListItem.SubItems[subItemIndex].Text = ItemText;
                    if (ListItem.SubItems[subItemIndex + 2].Text == "")
                    {
                        ListItem.SubItems[subItemIndex + 2].Text = "1";
                    }
                    else
                    {
                        ListItem.SubItems[subItemIndex + 2].Text = Convert.ToString(Convert.ToInt32(ListItem.SubItems[subItemIndex + 2].Text) + 1);
                    }
                }
                else 
                if (ListItem.SubItems[subItemIndex].Text != ItemText)
                {
                    ListItem.SubItems[subItemIndex].Text = ItemText;
                    ListItem.SubItems[subItemIndex+2].Text = "1";
                }
                else
                {
                    ListItem.SubItems[subItemIndex + 2].Text = Convert.ToString(Convert.ToInt32(ListItem.SubItems[subItemIndex + 2].Text) + 1);
                    if( (Convert.ToUInt32(ListItem.SubItems[subItemIndex + 2].Text)>9999))
                        ListItem.SubItems[subItemIndex + 2].Text="1";
                }

            }
            if (subItemIndex == 2)
            {
                if (ListItem.SubItems[subItemIndex].Text != ItemText)
                {
                    ListItem.SubItems[subItemIndex].Text = ItemText;
                }
            }

        }
        private void button2_Click(object sender, EventArgs e)
        {
            Timer_Test_.Enabled = !Timer_Test_.Enabled;
            if (!Timer_Test_.Enabled)
            {
                if (ListView1_EPC.Items.Count != 0)
                {
                    SpeedButton_Read_G2.Enabled = true;
                    Button_DataWrite.Enabled = true;
                    BlockWrite.Enabled = true;
                    Button_BlockErase.Enabled = true;
                    checkBox1.Enabled=true;
                }
                if (ListView1_EPC.Items.Count == 0)
                {
                    SpeedButton_Read_G2.Enabled = false;
                    Button_DataWrite.Enabled = false;
                    BlockWrite.Enabled = false;
                    Button_BlockErase.Enabled = false;
                    checkBox1.Enabled=false;

                }
                AddCmdLog("Inventory", "Detener", 0);
                button2.Text = "Consultar";
            }
            else
            {
                SpeedButton_Read_G2.Enabled = false;
                Button_DataWrite.Enabled = false;
                BlockWrite.Enabled = false;
                Button_BlockErase.Enabled = false;
                ListView1_EPC.Items.Clear();
                ComboBox_EPC2.Items.Clear();
                button2.Text = "detener";
                checkBox1.Enabled = false;
            }
        }
        private void Inventory()
        {
              int i;
              int CardNum=0;
              int Totallen = 0;
              int EPClen,m;
              byte[] EPC=new byte[5000];
              int CardIndex;
              string temps;
              string s, sEPC;
              bool isonlistview;
              fIsInventoryScan = true;
              byte AdrTID=0;
              byte LenTID = 0; 
              byte TIDFlag=0;

                AdrTID=0;
                LenTID=0;
                TIDFlag=0;

              ListViewItem aListItem = new ListViewItem();
              fCmdRet = StaticClassReaderB.Inventory_G2(ref fComAdr,AdrTID,LenTID,TIDFlag, EPC, ref Totallen, ref CardNum, frmcomportindex);      
             if ( (fCmdRet == 1)| (fCmdRet == 2)| (fCmdRet == 3)| (fCmdRet == 4)|(fCmdRet == 0xFB) )//代表已查找结束，
             {
                byte[] daw = new byte[Totallen];
                Array.Copy(EPC, daw, Totallen);               
                temps = ByteArrayToHexString(daw);
                fInventory_EPC_List = temps;            // Registros almacenados.
                m=0;
                
                 if (CardNum==0)
                 {
                     fIsInventoryScan = false;
                     return;
                 }

                 for (CardIndex = 0;CardIndex<CardNum;CardIndex++)
                 {
                     EPClen = daw[m];
                     sEPC = temps.Substring(m * 2 + 2, EPClen * 2);
                     m = m + EPClen + 1;
                     if (sEPC.Length != EPClen*2 )
                        return;

                    // Marco. Aca se leyó una etiqueta 
                    // EnviarPost(sEPC);
                    lector.RegistrarLectura( sEPC );

                    isonlistview = false;
                     for (i=0; i< ListView1_EPC.Items.Count;i++)     // Determinar si está en la lista Listview
                     { 
                        if (sEPC==ListView1_EPC.Items[i].SubItems[1].Text)
                        {
                         aListItem = ListView1_EPC.Items[i];
                         ChangeSubItem(aListItem, 1, sEPC);
                         isonlistview=true;
                        }
                      }
                      if (!isonlistview)
                      {
                          aListItem = ListView1_EPC.Items.Add((ListView1_EPC.Items.Count + 1).ToString());
                          aListItem.SubItems.Add("");
                          aListItem.SubItems.Add("");
                          aListItem.SubItems.Add("");
                          s = sEPC;
                          ChangeSubItem(aListItem, 1, s);
                          s = (sEPC.Length / 2).ToString().PadLeft(2, '0');
                          ChangeSubItem(aListItem, 2, s);
                         
                      }             
                 }            
            }
            
           
            fIsInventoryScan = false;
            if (fAppClosed)
                Close();
        }
        private void Timer_Test__Tick(object sender, EventArgs e)
        {
            if (fIsInventoryScan)
                return;           
            Inventory();
        }

        private void SpeedButton_Read_G2_Click(object sender, EventArgs e)
        {
            if (Edit_WordPtr.Text == "")
            {
                MessageBox.Show("La dirección de inicio está vacía", "Informacion");
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("Longitud de lectura/borrado de bloque", "Informacion");
                return;
            }
            if (Edit_AccessCode2.Text == "")
            {
                MessageBox.Show("La contraseña está vacía", "Informacion");
                return;
            }
            if (Convert.ToInt32(Edit_WordPtr.Text,16) + Convert.ToInt32(textBox1.Text) > 120)
                return;
               Timer_G2_Read.Enabled =!Timer_G2_Read.Enabled;
               if (Timer_G2_Read.Enabled)
               {
                   button2.Enabled = false;
                   Button_DataWrite.Enabled = false;
                   BlockWrite.Enabled = false;
                   Button_BlockErase.Enabled = false;
                   SpeedButton_Read_G2.Text = "detener";
               }
               else
               {
                   if (ListView1_EPC.Items.Count != 0)
                   {
                       button2.Enabled = true;
                   
                       Button_DataWrite.Enabled = true;
                       BlockWrite.Enabled = true;
                       Button_BlockErase.Enabled = true;
                   }
                   if (ListView1_EPC.Items.Count == 0)
                   {
                       button2.Enabled = true;
                       Button_DataWrite.Enabled = false;
                       BlockWrite.Enabled = false;
                       Button_BlockErase.Enabled = false;
                   }
                   SpeedButton_Read_G2.Text = "leer";
               }
        }

        private void Timer_G2_Read_Tick(object sender, EventArgs e)
        {
            if (fIsInventoryScan)
                return;
            fIsInventoryScan = true;
                byte WordPtr, ENum;
                byte Num = 0;
                byte Mem = 0;
                byte EPClength=0;
                string str;
                byte[] CardData=new  byte[320];
                if ((maskadr_textbox.Text=="")||(maskLen_textBox.Text=="") )            
              {
                  fIsInventoryScan = false;
                  return;
              }
              if (checkBox1.Checked)
              MaskFlag=1;
              else
              MaskFlag = 0;
              Maskadr = Convert.ToByte(maskadr_textbox.Text,16);
              MaskLen = Convert.ToByte(maskLen_textBox.Text,16);
              if (textBox1.Text == "")
              {
                  fIsInventoryScan = false;
                  return;
              }
                if (ComboBox_EPC2.Items.Count == 0)
                {
                    fIsInventoryScan = false;
                    return;
                }
                if (ComboBox_EPC2.SelectedItem == null)
                {
                    fIsInventoryScan = false;
                    return;
                }
                str = ComboBox_EPC2.SelectedItem.ToString();
                ENum = Convert.ToByte(str.Length / 4);
                EPClength = Convert.ToByte(str.Length / 2);
                byte[] EPC = new byte[ENum*2];
                EPC = HexStringToByteArray(str);
                if (C_Reserve.Checked)
                    Mem = 0;
                if (C_EPC.Checked)
                    Mem = 1;
                if (C_TID.Checked)
                    Mem = 2;
                if (C_User.Checked)
                    Mem = 3;
                if (Edit_AccessCode2.Text == "")
                {
                    fIsInventoryScan = false;
                    return;
                }
                if (Edit_WordPtr.Text == "")
                {
                    fIsInventoryScan = false;
                    return;
                }
                WordPtr = Convert.ToByte(Edit_WordPtr.Text, 16);
                Num = Convert.ToByte(textBox1.Text);
                if (Edit_AccessCode2.Text.Length != 8)
                {
                    fIsInventoryScan = false;
                    return;
                }
                fPassWord = HexStringToByteArray(Edit_AccessCode2.Text);
                fCmdRet = StaticClassReaderB.ReadCard_G2(ref fComAdr, EPC, Mem, WordPtr, Num, fPassWord,Maskadr,MaskLen,MaskFlag, CardData, EPClength, ref ferrorcode, frmcomportindex);
                if (fCmdRet == 0)
                {
                    byte[] daw = new byte[Num*2];
                    Array.Copy(CardData, daw, Num * 2);
                    listBox1.Items.Add(ByteArrayToHexString(daw));
                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                    AddCmdLog("ReadData", "Leer", fCmdRet);
                }
                if (ferrorcode != -1)
             {
                  StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() +
                   " 'leer' Error de retorno = 0x" + Convert.ToString(ferrorcode, 2) +
                   "(" + GetErrorCodeDesc(ferrorcode) + ")";
                    ferrorcode=-1;
             }
             fIsInventoryScan = false;
              if (fAppClosed)
                    Close();
        }

        private void Button_DataWrite_Click(object sender, EventArgs e)
        {
            byte WordPtr, ENum;
            byte Num = 0;
            byte Mem = 0;
            byte WNum = 0;
            byte EPClength = 0;
            byte Writedatalen = 0;
            int  WrittenDataNum = 0;
            string s2, str;
            byte[] CardData = new byte[320];
            byte[] writedata = new byte[230];
            if ((maskadr_textbox.Text == "") || (maskLen_textBox.Text == ""))
            {
                return;
            }
            if (checkBox1.Checked)
                MaskFlag = 1;
            else
                MaskFlag = 0;
            Maskadr = Convert.ToByte(maskadr_textbox.Text, 16);
            MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
            if (ComboBox_EPC2.Items.Count == 0)
                return;
            if (ComboBox_EPC2.SelectedItem == null)
                return;
            str = ComboBox_EPC2.SelectedItem.ToString();
            ENum = Convert.ToByte(str.Length / 4);
            EPClength = Convert.ToByte(ENum * 2);
            byte[] EPC = new byte[ENum];
            EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;
            if (Edit_WordPtr.Text == "")
            {
                MessageBox.Show("La dirección de inicio está vacía", "Informacion");
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("Longitud de lectura/borrado de bloque", "Informacion");
                return;
            }
            if (Convert.ToInt32(Edit_WordPtr.Text,16) + Convert.ToInt32(textBox1.Text) > 120)
                return;
            if (Edit_AccessCode2.Text == "")
            {
                return;
            }
            WordPtr = Convert.ToByte(Edit_WordPtr.Text, 16);
            Num = Convert.ToByte(textBox1.Text);
            if (Edit_AccessCode2.Text.Length != 8)
            {
                return;
            }
            fPassWord = HexStringToByteArray(Edit_AccessCode2.Text);
            if (Edit_WriteData.Text == "")
                return;
            s2 = Edit_WriteData.Text;
            if (s2.Length % 4 != 0)
            {
                MessageBox.Show("Entrada en unidades de palabras.", "Grabar");
                return;
            }
            WNum = Convert.ToByte(s2.Length / 4);
            byte[] Writedata = new byte[WNum * 2];
            Writedata = HexStringToByteArray(s2);
            Writedatalen = Convert.ToByte(WNum * 2);
            fCmdRet = StaticClassReaderB.WriteCard_G2(ref fComAdr, EPC, Mem, WordPtr, Writedatalen, Writedata, fPassWord,Maskadr,MaskLen,MaskFlag, WrittenDataNum, EPClength, ref ferrorcode, frmcomportindex);
            AddCmdLog("Write data", "Grabar", fCmdRet);
            if (fCmdRet == 0)
            {
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + "El comando 'Escribir EPC' devuelve = 0x00" +
                  "(EPC escrito con éxito)";
            }    
        }

        private void Button_BlockErase_Click(object sender, EventArgs e)
        {
            byte WordPtr, ENum;
            byte Num = 0;
            byte Mem = 0;
            byte EPClength = 0;
            string str;
            byte[] CardData = new byte[320];
            if ((maskadr_textbox.Text == "") || (maskLen_textBox.Text == ""))
            {
                fIsInventoryScan = false;
                return;
            }
            if (checkBox1.Checked)
                MaskFlag = 1;
            else
                MaskFlag = 0;
            Maskadr = Convert.ToByte(maskadr_textbox.Text,16);
            MaskLen = Convert.ToByte(maskLen_textBox.Text,16);
            if (ComboBox_EPC2.Items.Count == 0)
                return;
            if (ComboBox_EPC2.SelectedItem == null)
                return;
            str = ComboBox_EPC2.SelectedItem.ToString();
            if (str == "")
                return;
            ENum = Convert.ToByte(str.Length / 4);
            EPClength = Convert.ToByte(str.Length / 2);
            byte[] EPC = new byte[ENum];
            EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;
            if (Edit_WordPtr.Text == "")
            {
                MessageBox.Show("La dirección de inicio está vacía","Informacion");
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("Longitud de lectura/borrado de bloque", "Informacion");
                return;
            }
            if (Convert.ToInt32(Edit_WordPtr.Text,16) + Convert.ToInt32(textBox1.Text) > 120)
                return;
            if (Edit_AccessCode2.Text == "")
                return;
            WordPtr = Convert.ToByte(Edit_WordPtr.Text, 16);
            if ((Mem == 1) & (WordPtr < 2))
            {
                MessageBox.Show("La longitud de la dirección inicial para borrar el área EPC debe ser mayor o igual a 0x01. Vuelva a ingresar.！", "Informacion");
                return;
            }
            Num = Convert.ToByte(textBox1.Text);
            if (Edit_AccessCode2.Text.Length != 8)
            {
                return;
            }
            fPassWord = HexStringToByteArray(Edit_AccessCode2.Text);
            fCmdRet = StaticClassReaderB.EraseCard_G2(ref fComAdr, EPC, Mem, WordPtr, Num, fPassWord,Maskadr,MaskLen,MaskFlag,EPClength, ref ferrorcode, frmcomportindex);
            AddCmdLog("EraseCard", "块擦除", fCmdRet);
            if (fCmdRet == 0)
            {
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + "El comando Borrar datos devuelve = 0x00" +
                     "(Datos borrados exitosamente)";
            }       
        }

        private void button7_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }


               
        
        private void Timer_G2_Alarm_Tick(object sender, EventArgs e)
        {
            if (fIsInventoryScan)
                return;
            fIsInventoryScan = true;
             fCmdRet=StaticClassReaderB.CheckEASAlarm_G2(ref fComAdr,ref ferrorcode,frmcomportindex);
            if (fCmdRet==0)
            {
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + " El comando Detectar alarma EAS devuelve = 0x00 " +
                          "(Alarma EAS detectada)";
            }
            else
            {
              AddCmdLog("CheckEASAlarm_G2", "Detectar alarma EAS", fCmdRet);
            }
            fIsInventoryScan = false;
            if (fAppClosed)
                Close();
        }

        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Timer_Test_.Enabled = false;
            Timer_G2_Read.Enabled = false;
            Timer_G2_Alarm.Enabled = false;
            fAppClosed = true;
            StaticClassReaderB.CloseComPort();
        }

        private void ComboBox_IntervalTime_SelectedIndexChanged(object sender, EventArgs e)
        {
              if   (ComboBox_IntervalTime.SelectedIndex <6)
              Timer_Test_.Interval =100;
              else
              Timer_Test_.Interval =(ComboBox_IntervalTime.SelectedIndex+4)*10;
        }

       
        

        private void C_EPC_CheckedChanged(object sender, EventArgs e)
        {
            Edit_WordPtr.ReadOnly = false;
        }

        private void C_TID_CheckedChanged(object sender, EventArgs e)
        {
            if ((!Timer_Test_.Enabled) & (!Timer_G2_Alarm.Enabled) &(!Timer_G2_Read.Enabled))
            {
                if (ListView1_EPC.Items.Count != 0)
                    Button_DataWrite.Enabled = true;
            }
            Edit_WordPtr.ReadOnly = false;
        }

        private void C_User_CheckedChanged(object sender, EventArgs e)
        {
            if ((!Timer_Test_.Enabled) & (!Timer_G2_Alarm.Enabled) & (!Timer_G2_Read.Enabled))
            {
                if (ListView1_EPC.Items.Count != 0)
                    Button_DataWrite.Enabled = true;
            }
            Edit_WordPtr.ReadOnly = false;
        }

        private void C_Reserve_CheckedChanged(object sender, EventArgs e)
        {
            if ((!Timer_Test_.Enabled) & (!Timer_G2_Alarm.Enabled) &(!Timer_G2_Read.Enabled))
            {
                if (ListView1_EPC.Items.Count != 0)
                    Button_DataWrite.Enabled = true;
            }
            Edit_WordPtr.ReadOnly = false;
        }

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
                timer1.Enabled = false;

                Timer_G2_Alarm.Enabled = false;
                Timer_G2_Read.Enabled = false;
                Timer_Test_.Enabled = false;
                SpeedButton_Read_G2.Text = "Leer";
                button2.Text = "Consultar";
                if ((ListView1_EPC.Items.Count != 0)&&(ComOpen))
                {
                    button2.Enabled = true;
                    SpeedButton_Read_G2.Enabled = true;
                  //  if (C_EPC.Checked)
                  //      Button_DataWrite.Enabled = false;
                  //  else
                        Button_DataWrite.Enabled = true;
                        BlockWrite.Enabled = true;
                    Button_BlockErase.Enabled = true;
                    checkBox1.Enabled = true;
                }
                if ((ListView1_EPC.Items.Count == 0)&&(ComOpen))
                {
                    button2.Enabled = true;
                    SpeedButton_Read_G2.Enabled = false;
                    Button_DataWrite.Enabled = false;
                    BlockWrite.Enabled = false;
                    Button_BlockErase.Enabled = false;
                    checkBox1.Enabled = false;
                }
        }



        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox4.SelectedIndex == 0)
            {
                radioButton5.Enabled = false;
                radioButton6.Enabled = false;
                radioButton7.Enabled = false;
                radioButton8.Enabled = false;
                radioButton9.Enabled = false;
                radioButton10.Enabled = false;
                radioButton11.Enabled = false;
                radioButton12.Enabled = false;
                radioButton13.Enabled = false;
                radioButton14.Enabled = false;
                radioButton15.Enabled = false;
                radioButton16.Enabled = false;
                radioButton17.Enabled = false;
                radioButton18.Enabled = false;
                radioButton19.Enabled = false;
                radioButton20.Enabled = false;
                textBox3.Enabled = false;
                comboBox5.Enabled = false;
                comboBox6.Enabled = false;
            }
            if ((comboBox4.SelectedIndex == 1) | (comboBox4.SelectedIndex == 2) | (comboBox4.SelectedIndex == 3))
            {
                radioButton5.Enabled = true;
                radioButton6.Enabled = true;
                radioButton7.Enabled = true;
                radioButton8.Enabled = true;
                radioButton20.Enabled = true;
                comboBox5.Items.Clear();
                if (radioButton20.Checked)
                {
                    for (int i = 1; i < 5; i++)
                        comboBox5.Items.Add(Convert.ToString(i));
                    comboBox5.SelectedIndex = 3;
                    label42.Text = "bytes leidos";
                }
                else
                {
                    for (int i = 1; i < 33; i++)
                        comboBox5.Items.Add(Convert.ToString(i));
                    comboBox5.SelectedIndex = 0;
                    label42.Text = "Leer el recuento de palabras:";
                }

                if (radioButton7.Checked)
                {
                    radioButton16.Enabled = true;
                    radioButton17.Enabled = true;
                }
                else
                {
                    radioButton16.Enabled = false;
                    radioButton17.Enabled = false;
                }
                if (radioButton5.Checked)
                {
                    radioButton9.Enabled = true;
                    radioButton10.Enabled = true;
                    radioButton11.Enabled = true;
                    radioButton12.Enabled = true;
                    radioButton18.Enabled = true;
                    if (radioButton20.Checked)    //Syris485
                    {
                        radioButton13.Enabled = false;
                        radioButton19.Enabled = false;
                    }
                    else
                    {
                        radioButton13.Enabled = true;
                        radioButton19.Enabled = true;
                    }
                    if ((radioButton13.Checked) || (radioButton19.Checked))
                        comboBox6.Enabled = false;
                    else
                        comboBox6.Enabled = true;
                }
                else
                    comboBox6.Enabled = true;
                radioButton14.Enabled = true;
                radioButton15.Enabled = true;
                textBox3.Enabled = true;
                if (radioButton7.Checked)
                    comboBox5.Enabled = false;
                else
                    comboBox5.Enabled =true;
            }
            
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
                if ((comboBox4.SelectedIndex == 1) | (comboBox4.SelectedIndex == 2) | (comboBox4.SelectedIndex == 3))
                {
                    radioButton9.Enabled = true;
                    radioButton10.Enabled = true;
                    radioButton11.Enabled = true;
                    radioButton12.Enabled = true;
                    radioButton13.Enabled = true;
                    radioButton18.Enabled = true;
                    if (radioButton16.Checked)
                        label41.Text = "Dirección de la palabra inicial(Hex):";
                    else
                        label41.Text = "Dirección de byte inicial(Hex):";
                    if (radioButton20.Checked)
                    {
                      radioButton13.Enabled=false;
                      radioButton19.Enabled=false;
                       label41.Text="Dirección de byte inicial(Hex):";
                    }
                    else
                    {
                        radioButton13.Enabled=true;
                        radioButton19.Enabled=true;
                    }
                    if (radioButton7.Checked)
                    {
                        radioButton16.Enabled = true;
                        radioButton17.Enabled = true;
                        if ((radioButton13.Checked) || (radioButton19.Checked))
                        {
                            comboBox6.Enabled = false;
                        }
                        else
                        {
                            comboBox6.Enabled = true;
                        }
                       
                    }
                    else
                    {
                        radioButton16.Enabled = false;
                        radioButton17.Enabled = false;
                        if ((radioButton13.Checked) || (radioButton19.Checked))
                            comboBox6.Enabled = false;
                        else
                            comboBox6.Enabled = true;
                         if (radioButton20.Checked)
                          label41.Text="Dirección de byte inicial(Hex):";
                          else
                          label41.Text="Dirección de la palabra inicial(Hex):";
                    }
                }
            }
            else
            {
                radioButton9.Enabled = false;
                radioButton10.Enabled = false;
                radioButton11.Enabled = false;
                radioButton12.Enabled = false;
                radioButton13.Enabled = false;
                radioButton18.Enabled = false;
                radioButton16.Enabled = false;
                radioButton17.Enabled = false;
                radioButton19.Enabled = false;
                comboBox6.Enabled = true;
                label41.Text = "Dirección de byte inicial(Hex)";
            }

        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
              if((radioButton5.Checked)&&(comboBox4.SelectedIndex>0))
              {
                radioButton16.Enabled=true;
                radioButton17.Enabled=true;
                radioButton13.Enabled=true;
                radioButton19.Enabled=true;
                if(radioButton16.Checked)
                label41.Text="Dirección de la palabra inicial(Hex):";
                else
                label41.Text="Dirección de byte inicial(Hex):";
                label42.Text="Leer el recuento de palabras:";
              }
               comboBox5.Enabled=false;
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
             if((comboBox4.SelectedIndex==1)||(comboBox4.SelectedIndex==2)||(comboBox4.SelectedIndex==3))
             {
                  if(radioButton8.Checked)
                    comboBox5.Enabled=true;
                   comboBox5.Items.Clear();
                   if (radioButton20.Checked)
                   {
                      for(int i=1;i<5;i++)
                      comboBox5.Items.Add(Convert.ToString(i));
                      comboBox5.SelectedIndex=3;
                      label42.Text="bytes leidos";
                      comboBox5.Enabled=true;
                      label41.Text="Dirección de byte inicial(Hex):";
                   }
                   else
                   {
                      for (int i=1;i<33;i++)
                      comboBox5.Items.Add(Convert.ToString(i));
                      comboBox5.SelectedIndex=0;
                      label42.Text="Leer el recuento de palabras:";
                      label41.Text="Dirección de la palabra inicial(Hex):";
                   }
                if(radioButton5.Checked)
                {
                   radioButton16.Enabled=false;
                    radioButton17.Enabled=false;
                   if (radioButton20.Checked)
                   {
                      radioButton13.Enabled=false;
                      radioButton19.Enabled=false;
                   }
                   else
                   {
                     radioButton13.Enabled=true;
                     radioButton19.Enabled=true;
                   }
                }
                else
                {
                  label41.Text="Dirección de byte inicial(Hex):";
                  radioButton13.Enabled=false;
                  radioButton19.Enabled=false;
                }


             }
        }


        private void button8_Click(object sender, EventArgs e)
        {
            int Reader_bit0;
            int Reader_bit1;
            int Reader_bit2;
            int Reader_bit3;
            int Reader_bit4;
            byte[] Parameter = new byte[6];
            Parameter[0] = Convert.ToByte(comboBox4.SelectedIndex);
            if (radioButton5.Checked)
                Reader_bit0 = 0;
            else
                Reader_bit0 = 1;
            if (radioButton7.Checked)
                Reader_bit1 = 0;
            else
                Reader_bit1 = 1;
            if (radioButton14.Checked)
                Reader_bit2 = 0;
            else
                Reader_bit2 = 1;
            if (radioButton16.Checked)
                Reader_bit3 = 0;
            else
                Reader_bit3 = 1;
             if (radioButton20.Checked)
              Reader_bit4 = 1;
              else
                Reader_bit4 = 0 ;   
            Parameter[1] = Convert.ToByte(Reader_bit0 * 1 + Reader_bit1 * 2 + Reader_bit2 * 4 + Reader_bit3 * 8+ Reader_bit4 * 16);
            if (radioButton9.Checked)
                Parameter[2] = 0;
            if (radioButton10.Checked)
                Parameter[2] = 1;
            if (radioButton11.Checked)
                Parameter[2] = 2;
            if (radioButton12.Checked)
                Parameter[2] = 3;
            if (radioButton13.Checked)
                Parameter[2] = 4;
            if (radioButton18.Checked)
                Parameter[2] = 5;
            if (radioButton19.Checked)
                Parameter[2] = 6;
            if (textBox3.Text == "")
            {
                MessageBox.Show("¡La dirección del punto de entrada principal de la aplicación no puede estar vacía!", "Consejo");
                return;
            }
            Parameter[3] = Convert.ToByte(textBox3.Text, 16);
            Parameter[4] = Convert.ToByte(comboBox5.SelectedIndex + 1);
            Parameter[5] = Convert.ToByte(comboBox6.SelectedIndex); ;
            fCmdRet = StaticClassReaderB.SetWorkMode(ref fComAdr, Parameter, frmcomportindex);
            if (fCmdRet == 0)
            {
                if ((comboBox4.SelectedIndex == 1) | (comboBox4.SelectedIndex == 2) | (comboBox4.SelectedIndex == 3))
                {
                     if(radioButton6.Checked)
                    {
                       radioButton13.Enabled=false;
                       radioButton19.Enabled=false;
                    }
                    else
                    {
                        if (radioButton20.Checked)
                        {
                            radioButton13.Enabled = false;
                            radioButton19.Enabled = false;
                        }
                    }
                }
                if (comboBox4.SelectedIndex == 0)
                {
                    timer1.Enabled = false;
                }
            }
            AddCmdLog("SetWorkMode", "Config", fCmdRet);
        }


        private void GetData()
        {
            byte[] ScanModeData = new byte[40960];
          int ValidDatalength,i;
          string temp, temps;
          ValidDatalength = 0;
          fCmdRet = StaticClassReaderB.ReadActiveModeData(ScanModeData, ref ValidDatalength, frmcomportindex);
          if (fCmdRet == 0)
          { 
            temp="";
            temps=ByteArrayToHexString(ScanModeData);
            for(i=0;i<ValidDatalength;i++)
            {
                temp = temp + temps.Substring(i * 2, 2) + " ";
            }
          }
         // AddCmdLog("Get", "Conseguir", fCmdRet);
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (fIsInventoryScan)
                fIsInventoryScan = true;
            GetData();
            if (fAppClosed)
                Close();
            fIsInventoryScan = false;
        }

        
        private void radioButton_band1_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 63; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(902.6 + i * 0.4) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(902.6 + i * 0.4) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 62;
            ComboBox_dminfre.SelectedIndex = 0;
        }

        private void radioButton_band2_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 20; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(920.125 + i * 0.25) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(920.125 + i * 0.25) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 19;
            ComboBox_dminfre.SelectedIndex = 0;
        }

        private void radioButton_band3_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 50; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(902.75 + i * 0.5) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(902.75 + i * 0.5) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 49;
            ComboBox_dminfre.SelectedIndex = 0;
        }

        private void radioButton_band4_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 32; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(917.1 + i * 0.2) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(917.1 + i * 0.2) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 31;
            ComboBox_dminfre.SelectedIndex = 0;
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                maskadr_textbox.Enabled = true;
                maskLen_textBox.Enabled = true;
            }
            else
            {
                maskadr_textbox.Enabled = false;
                maskLen_textBox.Enabled = false;
            }
        }

        private void groupBox30_Enter(object sender, EventArgs e)
        {

        }

        private void radioButton16_CheckedChanged(object sender, EventArgs e)
        {
            label41.Text = "Dirección de la palabra inicial(Hex):";
        }

        private void radioButton17_CheckedChanged(object sender, EventArgs e)
        {
            label41.Text = "Dirección de byte inicial(Hex):";
        }

        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = true;
        }

        private void radioButton10_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = true;
        }

        private void radioButton11_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = true;
        }

        private void radioButton12_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = true;
        }

        private void radioButton18_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = true;
        }

        private void radioButton13_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = false;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            byte[] Parameter = new byte[12];

            fCmdRet = StaticClassReaderB.GetWorkModeParameter(ref fComAdr, Parameter, frmcomportindex);
            if (fCmdRet == 0)
            {
                comboBox4.SelectedIndex = Convert.ToInt32(Parameter[4]);
                if ((Parameter[4] == 1) || (Parameter[4] == 2) || (Parameter[4] == 3))
                {
                    radioButton5.Enabled = true;
                    radioButton6.Enabled = true;
                    radioButton7.Enabled = true;
                    radioButton8.Enabled = true;
                   
                    if (radioButton5.Checked)
                    {
                        if (radioButton7.Checked)
                        {
                            radioButton16.Enabled = true;
                            radioButton17.Enabled = true;
                        }
                        else
                        {
                            radioButton16.Enabled = false;
                            radioButton17.Enabled = false;
                        }
                        radioButton9.Enabled = true;
                        radioButton10.Enabled = true;
                        radioButton11.Enabled = true;
                        radioButton12.Enabled = true;
                        radioButton18.Enabled = true;
                        radioButton20.Enabled = true;
                        if (Convert.ToInt32((Parameter[5] & 0x10)) == 0x10) 
                        {
                          radioButton13.Enabled =false;
                          radioButton19.Enabled =false;
                        }
                         else
                        {
                            radioButton13.Enabled = true;
                            radioButton19.Enabled = true;
                        }
                        if ((radioButton13.Checked) || (radioButton19.Checked))
                            comboBox6.Enabled = false;
                        else
                            comboBox6.Enabled = true;
                    }
                    else
                        comboBox6.Enabled = true;
                    radioButton14.Enabled = true;
                    radioButton15.Enabled = true;
                    textBox3.Enabled = true;
                    if ((radioButton8.Checked)||(radioButton20.Checked))
                        comboBox5.Enabled = true;
                }
                if (Parameter[4] == 0)
                {
                    radioButton5.Enabled = false;
                    radioButton6.Enabled = false;
                    radioButton7.Enabled = false;
                    radioButton8.Enabled = false;
                    radioButton9.Enabled = false;
                    radioButton10.Enabled = false;
                    radioButton11.Enabled = false;
                    radioButton12.Enabled = false;
                    radioButton13.Enabled = false;
                    radioButton14.Enabled = false;
                    radioButton15.Enabled = false;
                    radioButton16.Enabled = false;
                    radioButton17.Enabled = false;
                    radioButton18.Enabled = false;
                    radioButton19.Enabled = false;
                    radioButton20.Enabled = false;
                    textBox3.Enabled = false;
                    comboBox5.Enabled = false;
                    comboBox6.Enabled = false;
                }
                if (Convert.ToInt32((Parameter[5]) & 0x01) == 0)
                    radioButton5.Checked = true;
                else
                    radioButton6.Checked = true;
                if (Convert.ToInt32((Parameter[5]) & 0x02) == 0)
                    radioButton7.Checked = true;
                else
                {
                    if (Convert.ToInt32((Parameter[5] & 0x10)) == 0) 
                    radioButton8.Checked=true;
                    else
                     radioButton20.Checked=true;
                }
                if (Convert.ToInt32((Parameter[5]) & 0x04) == 0)
                    radioButton14.Checked = true;
                else
                    radioButton15.Checked = true;
                if (Convert.ToInt32((Parameter[5]) & 0x08) == 0)
                    radioButton16.Checked = true;
                else
                    radioButton17.Checked = true;
                switch (Parameter[6])
                {
                    case 0:
                        radioButton9.Checked = true;
                        break;
                    case 1:
                        radioButton10.Checked = true;
                        break;
                    case 2:
                        radioButton11.Checked = true;
                        break;
                    case 3:
                        radioButton12.Checked = true;
                        break;
                    case 4:
                        radioButton13.Checked = true;
                        break;
                    case 5:
                        radioButton18.Checked = true;
                        break;
                    case 6:
                        radioButton19.Checked = true;
                        break;
                    default:
                        break;
                }
                textBox3.Text = Convert.ToString(Parameter[7], 16).PadLeft(2, '0');
                comboBox5.SelectedIndex = Convert.ToInt32(Parameter[8] - 1);
                comboBox6.SelectedIndex = Convert.ToInt32(Parameter[9]);
                comboBox7.SelectedIndex = Convert.ToInt32(Parameter[10]);
                comboBox_OffsetTime.SelectedIndex = Convert.ToInt32(Parameter[11]);
            }
            AddCmdLog("GetWorkModeParameter", "Obtener parámetros del modo de trabajo", fCmdRet);
        }

        private void radioButton19_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = false;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            byte Accuracy;
            Accuracy = Convert.ToByte(comboBox7.SelectedIndex);
            fCmdRet = StaticClassReaderB.SetAccuracy(ref fComAdr, Accuracy, frmcomportindex);
            AddCmdLog("SetAccuracy", "Precisión de la prueba ConfigEAS", fCmdRet);
        }


        

        private void button_OffsetTime_Click(object sender, EventArgs e)
        {
            byte OffsetTime;
            OffsetTime = Convert.ToByte(comboBox_OffsetTime.SelectedIndex);
            fCmdRet = StaticClassReaderB.SetOffsetTime(ref fComAdr, OffsetTime, frmcomportindex);
            AddCmdLog("SetOffsetTime", "Config", fCmdRet);
        }

        private void BlockWrite_Click(object sender, EventArgs e)
        {
            byte WordPtr, ENum;
            byte Num = 0;
            byte Mem = 0;
            byte WNum = 0;
            byte EPClength = 0;
            byte Writedatalen = 0;
            int WrittenDataNum = 0;
            string s2, str;
            byte[] CardData = new byte[320];
            byte[] writedata = new byte[230];
            if ((maskadr_textbox.Text == "") || (maskLen_textBox.Text == ""))
            {
                fIsInventoryScan = false;
                return;
            }
            if (checkBox1.Checked)
                MaskFlag = 1;
            else
                MaskFlag = 0;
            Maskadr = Convert.ToByte(maskadr_textbox.Text,16);
            MaskLen = Convert.ToByte(maskLen_textBox.Text,16);
            if (ComboBox_EPC2.Items.Count == 0)
                return;
            if (ComboBox_EPC2.SelectedItem == null)
                return;
            str = ComboBox_EPC2.SelectedItem.ToString();
            if (str == "")
                return;
            ENum = Convert.ToByte(str.Length / 4);
            EPClength = Convert.ToByte(ENum * 2);
            byte[] EPC = new byte[ENum];
            EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;
            if (Edit_WordPtr.Text == "")
            {
                MessageBox.Show("La dirección de inicio está vacía", "Informacion");
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("Longitud de lectura/borrado de bloque", "Informacion");
                return;
            }
            if (Convert.ToInt32(Edit_WordPtr.Text,16) + Convert.ToInt32(textBox1.Text) > 120)
                return;
            if (Edit_AccessCode2.Text == "")
            {
                return;
            }
            WordPtr = Convert.ToByte(Edit_WordPtr.Text, 16);
            Num = Convert.ToByte(textBox1.Text);
            if (Edit_AccessCode2.Text.Length != 8)
            {
                return;
            }
            fPassWord = HexStringToByteArray(Edit_AccessCode2.Text);
            if (Edit_WriteData.Text == "")
                return;
            s2 = Edit_WriteData.Text;
            if (s2.Length % 4 != 0)
            {
                MessageBox.Show("Entrada en unidades de palabras.", "Escritura en bloque");
                return;
            }
            WNum = Convert.ToByte(s2.Length / 4);
            byte[] Writedata = new byte[WNum * 2];
            Writedata = HexStringToByteArray(s2);
            Writedatalen = Convert.ToByte(WNum * 2);
            fCmdRet = StaticClassReaderB.WriteBlock_G2(ref fComAdr, EPC, Mem, WordPtr, Writedatalen, Writedata, fPassWord, Maskadr, MaskLen, MaskFlag, WrittenDataNum, EPClength, ref ferrorcode, frmcomportindex);
            AddCmdLog("Write Block", "Escritura en bloque", fCmdRet, ferrorcode);
            if (fCmdRet == 0)
            {
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + " El comando 'Escritura en bloque' devuelve = 0x00" +
                     "(Escritura de bloque exitosa)";
            }    
        }
    

    

        private void radioButton21_CheckedChanged(object sender, EventArgs e)
        {
             int i;
             ComboBox_dminfre.Items.Clear();
             ComboBox_dmaxfre.Items.Clear();
             for (i=0;i<15;i++)
             {
                 ComboBox_dminfre.Items.Add(Convert.ToString(865.1 + i * 0.2) + " MHz");
                 ComboBox_dmaxfre.Items.Add(Convert.ToString(865.1 + i * 0.2) + " MHz");
             }
             ComboBox_dmaxfre.SelectedIndex = 14;
             ComboBox_dminfre.SelectedIndex=0;
        }

        private void button_settigtime_Click(object sender, EventArgs e)
        {
            byte TriggerTime;
            TriggerTime = Convert.ToByte(comboBox_tigtime.SelectedIndex);
            fCmdRet = StaticClassReaderB.SetTriggerTime(ref fComAdr, ref TriggerTime, frmcomportindex);
            AddCmdLog("SetTriggerTime", "Tiempo de activación de la configuración", fCmdRet);
        }

        private void button_gettigtime_Click(object sender, EventArgs e)
        {
            byte TriggerTime;
            TriggerTime = 255;
            fCmdRet = StaticClassReaderB.SetTriggerTime(ref fComAdr, ref TriggerTime, frmcomportindex);
            if (fCmdRet==0)
            {
                comboBox_tigtime.SelectedIndex = TriggerTime;
            }
            AddCmdLog("SetTriggerTime", "Leer el tiempo de activación", fCmdRet);
        }






        private void OpenNetPort_Click(object sender, EventArgs e)
        {
            int port, openresult = 0;
            string IPAddr;
            fComAdr = Convert.ToByte(textBox9.Text, 16); // $FF;
            if ((textBox7.Text == "") || (textBox8.Text == ""))
                MessageBox.Show("Puerto de red，La IP no puede estar vacía!", "pista");
            port = Convert.ToInt32(textBox7.Text);
            IPAddr = textBox8.Text;
            openresult = StaticClassReaderB.OpenNetPort(port, IPAddr, ref fComAdr, ref frmcomportindex);
            fOpenComIndex = frmcomportindex;
            if (openresult == 0)
            {
                ComOpen = true;
                Button3_Click(sender, e); // Ejecutar automáticamente la lectura de la información del lector de tarjetas
            }
            if ((openresult == 0x35) || (openresult == 0x30))
            {
                MessageBox.Show("Error de comunicación TCP/IP", "información");
                StaticClassReaderB.CloseNetPort(frmcomportindex);
                ComOpen = false;
                return;
            }
            if ((fOpenComIndex != -1) && (openresult != 0X35) && (openresult != 0X30))
            {
                Button3.Enabled = true;
                Button5.Enabled = true;
                Button1.Enabled = true;
                button2.Enabled = true;
                button8.Enabled = true;
                button9.Enabled = true;
                button12.Enabled = true;
                button_OffsetTime.Enabled = true;
                button_settigtime.Enabled = true;
                button_gettigtime.Enabled = true;
                ComOpen = true;
            }
            if ((fOpenComIndex == -1) && (openresult == 0x30))
                MessageBox.Show("TCPIP Error de comunicación", "información");
            

            // Marco. Cambiar de pestaña
            tabControl1.SelectedIndex = 1;

            // marco. Comenzar a escuchar
            button2_Click(null, EventArgs.Empty);


        }

        private void CloseNetPort_Click(object sender, EventArgs e)
        {
            ClearLastInfo();
            fCmdRet = StaticClassReaderB.CloseNetPort(frmcomportindex);
            if (fCmdRet == 0)
            {
                fOpenComIndex = -1;
                
                Button3.Enabled = false;
                Button5.Enabled = false;
                Button1.Enabled = false;
                button2.Enabled = false;
                SpeedButton_Read_G2.Enabled = false;
                Button_DataWrite.Enabled = false;
                BlockWrite.Enabled = false;
                Button_BlockErase.Enabled = false;

                button8.Enabled = false;
                button9.Enabled = false;
                
                SpeedButton_Read_G2.Enabled = false;
                Button_DataWrite.Enabled = false;
                BlockWrite.Enabled = false;
                Button_BlockErase.Enabled = false;
                ListView1_EPC.Items.Clear();
                ComboBox_EPC2.Items.Clear();
                button2.Text = "Stop";
                checkBox1.Enabled = false;
                
                ComOpen = false;
                button12.Enabled = false;
                timer1.Enabled = false;
                comboBox4.SelectedIndex = 0;
                button_OffsetTime.Enabled = false;
                button_settigtime.Enabled = false;
                button_gettigtime.Enabled = false;
            }
        }

        
        private void radioButton21_CheckedChanged_1(object sender, EventArgs e)
        {
            OpenNetPort.Enabled = true;
            CloseNetPort.Enabled = true;
        }

    }




    public class RFIDCollector
    {
        private readonly object _lock = new object();
        private HashSet<string> _etiquetas = new HashSet<string>();
        private System.Threading.Timer _timer;
        private int _timeoutMs = 3000; // 3 segundos
        private bool _enviando = false;

        public RFIDCollector()
        {
            // Timer desactivado por defecto
            _timer = new System.Threading.Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Se llama cada vez que se detecta una etiqueta RFID.
        /// </summary>
        public void RegistrarLectura(string codigoEtiqueta)
        {
            lock (_lock)
            {
                _etiquetas.Add(codigoEtiqueta);

                // Reiniciar el timer: cuenta desde cero
                _timer.Change(_timeoutMs, Timeout.Infinite);
            }

            // Console.WriteLine("Etiqueta detectada: " + codigoEtiqueta);
        }

        private void OnTimerElapsed(object state)
        {
            lock (_lock)
            {
                if (_enviando || _etiquetas.Count == 0)
                    return;

                _enviando = true;
            }

            HashSet<string> lote;
            lock (_lock)
            {
                lote = new HashSet<string>(_etiquetas);
                _etiquetas.Clear();
            }

            // Enviar en otro hilo (para no bloquear el programa principal)
            ThreadPool.QueueUserWorkItem(delegate { EnviarLote(lote); });
        }

        private void EnviarLote(HashSet<string> lote)
        {
            try
            {
                Console.WriteLine("Enviando lote de " + lote.Count + " etiquetas...");

                string url = "https://mpb36649e50544b887b2.free.beeceptor.com"; // tu endpoint

                var datos = new
                {
                    user = "marcoAS",
                    pass = "1234",
                    tagIDs = lote.ToList()
                };

                string body = Newtonsoft.Json.JsonConvert.SerializeObject(datos);

                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";

                    // Esto es sincrónico, pero al ejecutarse en un hilo aparte, no bloquea la UI
                    string response = client.UploadString(url, "POST", body);

                    Console.WriteLine("Respuesta del servidor: " + response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar lote: " + ex.Message);
            }
            finally
            {
                lock (_lock)
                {
                    _enviando = false;
                }
            }
        }
    }



}