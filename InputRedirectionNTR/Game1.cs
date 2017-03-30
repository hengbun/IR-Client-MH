using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Net;

namespace InputRedirectionNTR
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D Font;
        Texture2D Cursor;
        Texture2D Background;
        IPAddress ipAddress;
        string IPAddress = "192.168.0.17";
        byte[] data = new byte[12];
        uint oldbuttons = 0xFFF;
        uint newbuttons = 0xFFF;
        uint oldtouch = 0x2000000;
        uint newtouch = 0x2000000;
        uint macrotouch = 0x2000000;
        uint oldcpad = 0x800800;
        uint newcpad = 0x800800;
        uint touchclick = 0x00;
        uint cpadclick = 0x00;
        int Mode = 0;
        Keys[] ipKeysToCheck = { Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5, Keys.NumPad6, Keys.NumPad7, Keys.NumPad8, Keys.NumPad9, Keys.Decimal, Keys.OemPeriod, Keys.Back, Keys.Delete, Keys.Escape };
        Keys[] buttonKeysToCheck = { Keys.A, Keys.B, Keys.RightShift, Keys.LeftShift, Keys.Enter, Keys.Right, Keys.Left, Keys.Up, Keys.Down, Keys.R, Keys.L, Keys.X, Keys.Y, Keys.Escape };
        Keys[] KeyboardInput = { Keys.A, Keys.S, Keys.N, Keys.M, Keys.H, Keys.F, Keys.T, Keys.G, Keys.W, Keys.Q, Keys.Z, Keys.X, Keys.Right, Keys.Left, Keys.Up, Keys.Down };
        uint[] GamePadInput = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x020, 0x40, 0x80, 0x100, 0x200, 0x400, 0x800 };
        uint[] MacroInput = { 0x10CEDE4, 0xDA7DB0, 0x18F8DE4, 0x13F3DE4 };
        string[] ButtonNames = { "A", "B", "Select", "Start", "DPad Right", "DPad Left", "DPad Up", "DPad Down", "R", "L", "X", "Y" };
        Keys UpKey;
        bool WaitForKeyUp;
        bool debug = false;
        KeyboardState keyboardState;
        GamePadState gamePadState;
        uint KeyIndex;
        Keys OldKey;
        uint OldButton;
        uint OldMacro;
        uint seconds = 0;
        bool useGamePad = true;
        bool Rstickcam = true;
        float thumbstickTolerance = 0.25f;
        string version = "MH01";

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            this.IsMouseVisible = true;
            this.Window.Title = "InputRedirection";
            graphics.PreferredBackBufferWidth = 320;
            graphics.PreferredBackBufferHeight = 240;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            if (File.Exists("config.cfg"))
            {
                ReadConfig();
            }
            else
            {
                SaveConfig();
            }

            spriteBatch = new SpriteBatch(GraphicsDevice);
            Font = Content.Load<Texture2D>("Fonts\\NESFont");
            Cursor = Content.Load<Texture2D>("Cursors\\Cursor");
            Background = Content.Load<Texture2D>("Background\\Background");
        }

        protected override void UnloadContent()
        {

        }

        protected override void Update(GameTime gameTime)
        {
            if (gameTime.TotalGameTime.TotalSeconds != seconds)
            {
                seconds = (uint)gameTime.TotalGameTime.TotalSeconds;
                Program.ntrClient.sendHeartbeatPacket();

            }

            switch (Mode)
            {
                case 0:
                    {
                        IsMouseVisible = !debug;
                        ReadMain();
                    }
                    break;

                case 1:
                    {
                        IsMouseVisible = true;
                        ReadIPInput();
                    }
                    break;

                case 2:
                    {
                        IsMouseVisible = true;
                        ReadKeyboardInput();
                    }
                    break;

                case 3:
                    {
                        IsMouseVisible = true;
                        ReadGamePadInput();
                    }
                    break;

                case 4:
                    {
                        IsMouseVisible = true;
                        ReadNewKey();
                    }
                    break;

                case 5:
                    {
                        Console.WriteLine("Mode: " + Mode);
                        IsMouseVisible = true;
                        ReadNewButton();
                    }
                    break;
                case 6:
                    {
                        Console.WriteLine("Mode: " + Mode);
                        IsMouseVisible = true;
                        ReadGamePadMacro();
                    }
                    break;
                case 7:
                    {
                        Console.WriteLine("Mode: " + Mode);
                        IsMouseVisible = true;
                        ReadNewMacro();
                    }
                    break;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();
            {
                switch (Mode)
                {
                    case 0:
                        {
                            ShowMain();
                        }
                        break;

                    case 1:
                        {
                            ShowIPInput();
                        }
                        break;

                    case 2:
                    case 4:
                        {
                            ShowKeyboardInput();
                        }
                        break;

                    case 3:
                    case 5:
                        {
                            ShowGamePadInput();
                            Console.WriteLine("Mode: " + Mode);
                        }
                        break;

                    case 6:
                        {
                            ShowMacroMenu();
                        }
                        break;

                    case 7:
                        {
                            ShowTouchScreenPic();
                        }
                        break;
                }
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void OnExiting(Object sender, EventArgs args)
        {
            Program.scriptHelper.disconnect();
        }

        private void ReadConfig()
        {
            StreamReader sr = new StreamReader("config.cfg");

            if (version == sr.ReadLine())
            {
                IPAddress = sr.ReadLine();
                if (sr.ReadLine() == "True")
                {
                    debug = true;
                    this.IsMouseVisible = false;
                }

                if (sr.ReadLine() == "False")
                {
                    useGamePad = false;
                }

                if (sr.ReadLine() == "False")
                {
                    Rstickcam = false;
                }

                for (int i = 0; i < KeyboardInput.Length; i++)
                {
                    KeyboardInput[i] = (Keys)Enum.Parse(typeof(Keys), sr.ReadLine());
                }

                for (int i = 0; i < GamePadInput.Length; i++)
                {
                    GamePadInput[i] = Convert.ToUInt32(sr.ReadLine());
                }

                for (int i = 0; i < MacroInput.Length; i++ )
                {
                    MacroInput[i] = Convert.ToUInt32(sr.ReadLine());
                }
                sr.Close();
            }
            else
            {
                sr.Close();
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            StreamWriter sw = new StreamWriter("config.cfg");

            sw.WriteLine(version);
            sw.WriteLine(IPAddress);
            sw.WriteLine(debug);
            sw.WriteLine(useGamePad);
            sw.WriteLine(Rstickcam);

            for (int i = 0; i < KeyboardInput.Length; i++)
            {
                sw.WriteLine(KeyboardInput[i]);
            }

            for (int i = 0; i < GamePadInput.Length; i++)
            {
                sw.WriteLine(GamePadInput[i]);
            }

            for (int i = 0; i < MacroInput.Length; i++)
            {
                sw.WriteLine(MacroInput[i]);
            }

            sw.Close();
        }

        private void CheckConnection()
        {
            if (!Program.Connected)
            {
                Program.scriptHelper.connect(IPAddress, 8000);
            }
        }

        private void ReadNewKey()
        {
            if (!WaitForKeyUp)
            {
                keyboardState = Keyboard.GetState();

                if (keyboardState.GetPressedKeys().Length > 0)
                {
                    switch (keyboardState.GetPressedKeys()[0])
                    {
                        case Keys.Escape:
                            {
                                KeyboardInput[KeyIndex] = OldKey;
                                Mode = 2;
                            }
                            break;

                        case Keys.F1:
                        case Keys.F2:
                        case Keys.F3:
                        case Keys.F4:
                        case Keys.F5:
                        case Keys.F6:
                            {
                                break;
                            }

                        default:
                            {
                                for (int i = 0; i < KeyboardInput.Length; i++)
                                {
                                    if (keyboardState.GetPressedKeys()[0] == KeyboardInput[i])
                                    {
                                        break;
                                    }

                                    if (i == (KeyboardInput.Length - 1))
                                    {
                                        KeyboardInput[KeyIndex] = keyboardState.GetPressedKeys()[0];
                                        Mode = 2;
                                        WaitForKeyUp = true;
                                        UpKey = keyboardState.GetPressedKeys()[0];
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            else
            {
                if (Keyboard.GetState().IsKeyUp(UpKey))
                {
                    WaitForKeyUp = false;
                }
            }
        }

        private void ReadNewButton() //Map New Gamepad Input from Keyboard Input Mode 5
        {
            if (!WaitForKeyUp)
            {
                for (int i = 0; i < buttonKeysToCheck.Length; i++)
                {
                    if (Keyboard.GetState().IsKeyDown(buttonKeysToCheck[i]))
                    {
                        WaitForKeyUp = true;
                        UpKey = buttonKeysToCheck[i];
                        switch (buttonKeysToCheck[i])
                        {
                            case Keys.Escape:
                                {
                                    if (System.Net.IPAddress.TryParse(IPAddress, out ipAddress))
                                    {
                                        GamePadInput[KeyIndex] = OldButton;
                                        Mode = 3;
                                    }
                                }
                                break;

                            default:
                                {
                                    switch (buttonKeysToCheck[i])
                                    {
                                        case Keys.A:
                                            {
                                                GamePadInput[KeyIndex] = 0x01;
                                                Mode = 3;
                                            }
                                            break;

                                        case Keys.B:
                                            {
                                                GamePadInput[KeyIndex] = 0x02;
                                                Mode = 3;
                                            }
                                            break;

                                        case Keys.RightShift:
                                        case Keys.LeftShift:
                                            {
                                                GamePadInput[KeyIndex] = 0x04;
                                                Mode = 3;
                                            }
                                            break;

                                        case Keys.Enter:
                                            {
                                                GamePadInput[KeyIndex] = 0x08;
                                                Mode = 3;
                                            }
                                            break;

                                        case Keys.Right:
                                            {
                                                GamePadInput[KeyIndex] = 0x10;
                                                Mode = 3;
                                            }
                                            break;

                                        case Keys.Left:
                                            {
                                                GamePadInput[KeyIndex] = 0x20;
                                                Mode = 3;
                                            }
                                            break;

                                        case Keys.Up:
                                            {
                                                GamePadInput[KeyIndex] = 0x40;
                                                Mode = 3;
                                            }
                                            break;

                                        case Keys.Down:
                                            {
                                                GamePadInput[KeyIndex] = 0x80;
                                                Mode = 3;
                                            }
                                            break;

                                        case Keys.R:
                                            {
                                                GamePadInput[KeyIndex] = 0x100;
                                                Mode = 3;
                                            }
                                            break;

                                        case Keys.L:
                                            {
                                                GamePadInput[KeyIndex] = 0x200;
                                                Mode = 3;
                                            }
                                            break;

                                        case Keys.X:
                                            {
                                                GamePadInput[KeyIndex] = 0x400;
                                                Mode = 3;
                                            }
                                            break;

                                        case Keys.Y:
                                            {
                                                GamePadInput[KeyIndex] = 0x800;
                                                Mode = 3;
                                            }
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            else
            {
                if (Keyboard.GetState().IsKeyUp(UpKey))
                {
                    WaitForKeyUp = false;
                }
            }
        }

        private void ReadNewMacro() //Map New Macro Mode 7
        {
            if (!WaitForKeyUp)
            {
                 if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                                    {
                                        Console.WriteLine(macrotouch);
                                        WaitForKeyUp = true;
                                        TouchInput(ref macrotouch, ref touchclick, false);
                                        MacroInput[KeyIndex] = macrotouch;
                                        Mode = 6;
                                    }
                 else if (Keyboard.GetState().IsKeyDown(Keys.Back))
                                     {
                                      if (System.Net.IPAddress.TryParse(IPAddress, out ipAddress))
                                             {
                                                Console.WriteLine("EscMode7");
                                                MacroInput[KeyIndex] = OldMacro;
                                                Mode = 6;
                                             }

                                        }
                
                  else
                                      {
                                        touchclick = 0x00;
                                        macrotouch = 0x2000000;
                                    }
                                }
            else
            {
                if (Keyboard.GetState().IsKeyUp(Keys.Escape))
                {
                    WaitForKeyUp = false;
                }
            }
        }

        private void ReadMain()
        {
            if (!WaitForKeyUp)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.F1))
                {
                    WaitForKeyUp = true;
                    UpKey = Keys.F1;
                    Mode = 1;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.F2))//KeyboardSetting
                {
                    WaitForKeyUp = true;
                    UpKey = Keys.F2;
                    Mode = 2;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.F3)) //GamePadSetting
                {
                    Console.WriteLine("F3 Pressed");
                    WaitForKeyUp = true;
                    UpKey = Keys.F3;
                    Mode = 3;
                    Console.WriteLine("Mode: "+Mode);
                }

                if (Keyboard.GetState().IsKeyDown(Keys.F4))
                {
                    WaitForKeyUp = true;
                    UpKey = Keys.F4;
                    debug = !debug;
                    this.IsMouseVisible = !this.IsMouseVisible;
                    SaveConfig();
                }

                if (Keyboard.GetState().IsKeyDown(Keys.F5))
                {
                    WaitForKeyUp = true;
                    UpKey = Keys.F5;
                    useGamePad = !useGamePad;
                    SaveConfig();
                }

                if (Keyboard.GetState().IsKeyDown(Keys.F6))
                {
                    WaitForKeyUp = true;
                    UpKey = Keys.F6;
                    Rstickcam = !Rstickcam;
                    SaveConfig();
                }

                if (Keyboard.GetState().IsKeyDown(Keys.F7)) //GamePadSetting
                {
                    Console.WriteLine("F6 Pressed");
                    WaitForKeyUp = true;
                    UpKey = Keys.F7;
                    Mode = 6;
                    Console.WriteLine("Mode: " + Mode);
                }
            }
            else
            {
                if (Keyboard.GetState().IsKeyUp(UpKey))
                {
                    WaitForKeyUp = false;
                }
            }

            keyboardState = Keyboard.GetState();
            gamePadState = GamePad.GetState(PlayerIndex.One);
            newbuttons = 0x00;
            //Keyboard
            for (int i = 0; i < GamePadInput.Length; i++)
            {
                if (keyboardState.IsKeyDown(KeyboardInput[i]))
                {
                    newbuttons += (uint)(0x01 << i);
                }
            }

            //GamePad
            if (useGamePad)
            {
                if (Rstickcam)
                {
                    RSticktoDpad();
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.B == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[0]) != GamePadInput[0])
                    {
                        newbuttons += GamePadInput[0];
                    }
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[1]) != GamePadInput[1])
                    {
                        newbuttons += GamePadInput[1];
                    }
                }


                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[2]) != GamePadInput[2])
                    {
                        newbuttons += GamePadInput[2];
                    }
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.Start == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[3]) != GamePadInput[3])
                    {
                        newbuttons += GamePadInput[3];
                    }
                }

                if (GamePad.GetState(PlayerIndex.One).DPad.Right == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[4]) != GamePadInput[4])
                    {
                        newbuttons += GamePadInput[4];
                    }
                }

                if (GamePad.GetState(PlayerIndex.One).DPad.Left == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[5]) != GamePadInput[5])
                    {
                        newbuttons += GamePadInput[5];
                    }
                }

                if (GamePad.GetState(PlayerIndex.One).DPad.Up == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[6]) != GamePadInput[6])
                    {
                        newbuttons += GamePadInput[6];
                    }
                }

                if (GamePad.GetState(PlayerIndex.One).DPad.Down == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[7]) != GamePadInput[7])
                    {
                        newbuttons += GamePadInput[7];
                    }
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.RightShoulder == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[8]) != GamePadInput[8])
                    {
                        newbuttons += GamePadInput[8];
                    }
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.LeftShoulder == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[9]) != GamePadInput[9])
                    {
                        newbuttons += GamePadInput[9];
                    }
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.Y == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[10]) != GamePadInput[10])
                    {
                        newbuttons += GamePadInput[10];
                    }
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.X == ButtonState.Pressed)
                {
                    if ((newbuttons & GamePadInput[11]) != GamePadInput[11])
                    {
                        newbuttons += GamePadInput[11];
                    }
                }
            }

            newbuttons ^= 0xFFF;

            //Touch
            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                TouchInput(ref newtouch, ref touchclick, false);
                Console.WriteLine(newtouch);
            }
            else
            {
                touchclick = 0x00;
                if (useGamePad)
                {
                    if (GamePad.GetState(PlayerIndex.One).Buttons.RightStick == ButtonState.Pressed)
                    {
                        if (Rstickcam) //Rstickcam true
                        {
                            newtouch = MacroInput[3];
                        }
                        else //false
                        {
                            newtouch = (uint)Math.Round(2047.5 + (GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.X * 2047.5));
                            newtouch += (uint)Math.Round(2047.5 - (GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.Y * 2047.5)) << 0x0C;
                            newtouch += 0x1000000;
                        }
                    }

                    else if ((GamePad.GetState(PlayerIndex.One).Triggers.Left)>=0.5){
                        newtouch = MacroInput[0];
                    }

                    else if ((GamePad.GetState(PlayerIndex.One).Triggers.Right) >= 0.5)
                    {
                        newtouch = MacroInput[1];
                    }

                    else if (GamePad.GetState(PlayerIndex.One).Buttons.LeftStick == ButtonState.Pressed)
                    {
                        newtouch = MacroInput[2];
                    }

                    else
                    {
                        newtouch = 0x2000000;
                    }
                }
                else
                {
                    newtouch = 0x2000000;
                }
            }

            //Circle Pad
            if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                TouchInput(ref newcpad, ref cpadclick, true);
            }
            else
            {
                cpadclick = 0x00;
                newcpad = (uint)Math.Round(2047.5 + (GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.X * 2047.5));
                newcpad += (uint)Math.Round(4095 - (2047.5 - (GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y * 2047.5))) << 0x0C;

                if (newcpad == 0x800800)
                {

                    if (Keyboard.GetState().IsKeyDown(KeyboardInput[12]))
                    {
                        newcpad = 0xFFF + (((newcpad >> 0x0C) & 0xFFF) << 0x0C);
                    }

                    if (Keyboard.GetState().IsKeyDown(KeyboardInput[13]))
                    {
                        newcpad = (((newcpad >> 0x0C) & 0xFFF) << 0x0C);
                    }

                    if (Keyboard.GetState().IsKeyDown(KeyboardInput[15]))
                    {
                        newcpad = (newcpad & 0xFFF) + (0x00 << 0x0C);
                    }

                    if (Keyboard.GetState().IsKeyDown(KeyboardInput[14]))
                    {
                        newcpad = (newcpad & 0xFFF) + (0xFFF << 0x0C);
                    }
                }

                if (newcpad != 0x800800)
                {
                    newcpad += 0x1000000;
                }
            }

            SendInput();
        }

        private void ReadIPInput()
        {
            if (!WaitForKeyUp)
            {
                for (int i = 0; i < ipKeysToCheck.Length; i++)
                {
                    if (Keyboard.GetState().IsKeyDown(ipKeysToCheck[i]))
                    {
                        WaitForKeyUp = true;
                        UpKey = ipKeysToCheck[i];
                        switch (ipKeysToCheck[i])
                        {
                            case Keys.Back:
                            case Keys.Delete:
                                {
                                    if (IPAddress.Length != 0)
                                    {
                                        IPAddress = IPAddress.Substring(0, IPAddress.Length - 1);
                                    }
                                }
                                break;

                            case Keys.Escape:
                                {
                                    if (System.Net.IPAddress.TryParse(IPAddress, out ipAddress))
                                    {
                                        Mode = 0;
                                        IPAddress = ipAddress.ToString();
                                        SaveConfig();
                                        Program.scriptHelper.disconnect();
                                    }
                                }
                                break;

                            default:
                                {
                                    if (IPAddress.Length < 15)
                                    {
                                        IPAddress += KeytoText(ipKeysToCheck[i]);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            else
            {
                if (Keyboard.GetState().IsKeyUp(UpKey))
                {
                    WaitForKeyUp = false;
                }
            }
        }

        private void ReadKeyboardInput()
        {
            if (!WaitForKeyUp)
            {
                for (int i = 0; i < KeyboardInput.Length; i++)
                {
                    if (Keyboard.GetState().IsKeyDown(KeyboardInput[i]))
                    {
                        Mode = 4;
                        WaitForKeyUp = true;
                        UpKey = KeyboardInput[i];
                        OldKey = KeyboardInput[i];
                        KeyboardInput[i] = Keys.None;
                        KeyIndex = (uint)i;
                        
                    }
                }

                if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                {
                    Mode = 0;
                    WaitForKeyUp = true;
                    UpKey = Keys.Escape;
                    SaveConfig();
                }
            }
            else
            {
                if (Keyboard.GetState().IsKeyUp(UpKey))
                {
                    WaitForKeyUp = false;
                }
            }
        }

        private void ReadGamePadInput() //First Step, where you press the gamepad button you want to remap Mode3
        {
            if (!WaitForKeyUp)
            {
                if (GamePad.GetState(PlayerIndex.One).Buttons.B == ButtonState.Pressed)
                {
                    Mode = 5;
                    KeyIndex = 0;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed)
                {
                    Mode = 5;
                    KeyIndex = 1;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }


                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                {
                    Mode = 5;
                    KeyIndex = 2;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.Start == ButtonState.Pressed)
                {
                    Mode = 5;
                    KeyIndex = 3;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }

                if (GamePad.GetState(PlayerIndex.One).DPad.Right == ButtonState.Pressed)
                {
                    Mode = 5;
                    KeyIndex = 4;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }

                if (GamePad.GetState(PlayerIndex.One).DPad.Left == ButtonState.Pressed)
                {
                    Mode = 5;
                    KeyIndex = 5;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }

                if (GamePad.GetState(PlayerIndex.One).DPad.Up == ButtonState.Pressed)
                {

                    Mode = 5;
                    KeyIndex = 6;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }

                if (GamePad.GetState(PlayerIndex.One).DPad.Down == ButtonState.Pressed)
                {
                    Mode = 5;
                    KeyIndex = 7;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.RightShoulder == ButtonState.Pressed)
                {
                    Mode = 5;
                    KeyIndex = 8;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.LeftShoulder == ButtonState.Pressed)
                {
                    Mode = 5;
                    KeyIndex = 9;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.Y == ButtonState.Pressed)
                {
                    Mode = 5;
                    KeyIndex = 10;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.X == ButtonState.Pressed)
                {
                    Mode = 5;
                    KeyIndex = 11;
                    OldButton = GamePadInput[KeyIndex];
                    GamePadInput[KeyIndex] = 0;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                {
                    Mode = 0;
                    WaitForKeyUp = true;
                    UpKey = Keys.Escape;
                    SaveConfig();
                }
            }
            else
            {
                if (Keyboard.GetState().IsKeyUp(UpKey))
                {
                    WaitForKeyUp = false;
                }
            }
        }

        private void ReadGamePadMacro() //First Step, where you press the gamepad button you want to remap for macro Mode 6
        {
            Console.WriteLine("Waitkey " + WaitForKeyUp);
            if (!WaitForKeyUp)
            {
                if (GamePad.GetState(PlayerIndex.One).Triggers.Left >= 0.5)
                {
                    KeyIndex = 0;
                    OldMacro = MacroInput[KeyIndex];
                    MacroInput[KeyIndex] = 0;
                    Mode = 7;
                }

                if (GamePad.GetState(PlayerIndex.One).Triggers.Right >=0.5)
                {
                    Mode = 7;
                    KeyIndex = 1;
                    OldMacro = MacroInput[KeyIndex];
                    MacroInput[KeyIndex] = 0;
                }


                if (GamePad.GetState(PlayerIndex.One).Buttons.LeftStick == ButtonState.Pressed)
                {
                    Mode = 7;
                    KeyIndex = 2;
                    OldMacro = MacroInput[KeyIndex];
                    Console.WriteLine(OldMacro);
                    MacroInput[KeyIndex] = 0;
                    
                }

                if (GamePad.GetState(PlayerIndex.One).Buttons.RightStick == ButtonState.Pressed)
                {
                    Mode = 7;
                    KeyIndex = 3;
                    OldMacro = MacroInput[KeyIndex];
                    MacroInput[KeyIndex] = 0;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                {
                    Console.WriteLine("EscMode6");
                    Mode = 0;
                    WaitForKeyUp = true;
                    UpKey = Keys.Escape;
                    SaveConfig();
                }
            }
            else
            {
                if (Keyboard.GetState().IsKeyUp(UpKey))
                {
                    WaitForKeyUp = false;
                }
            }
        }

        private void ShowMain()
        {
            if (debug)
            {
                spriteBatch.Draw(Background, new Rectangle(0, 0, 320, 240), Color.White);
                DrawString(8, 8, "GamePad   : " + useGamePad, Color.White);
                DrawString(8, 16, "IPAddress : " + IPAddress, Color.White);
                DrawString(8, 24, "Buttons   : " + oldbuttons.ToString("X8"), Color.White);
                DrawString(8, 32, "Touch     : " + oldtouch.ToString("X8"), Color.White);
                DrawString(8, 40, "CPad      : " + oldcpad.ToString("X8"), Color.White);
                DrawString(8, 48, "RStickCam : " + Rstickcam, Color.White);

                int mousex = Mouse.GetState().Position.X;
                int mousey = Mouse.GetState().Position.Y;
                if (oldtouch == 0x2000000)
                {
                    if ((GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.X == 0.0) && (GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.Y == 0.0))
                    {
                        if (MouseInWindow(mousex, mousey))
                        {
                            spriteBatch.Draw(Cursor, new Rectangle(mousex - 1, mousey - 1, 3, 3), Color.Red);
                        }
                    }
                    else
                    {
                        int stickx = (int)Math.Round(159.5 + (GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.X * 159.5));
                        int sticky = (int)Math.Round(119.5 - (GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.Y * 119.5));
                        spriteBatch.Draw(Cursor, new Rectangle(stickx - 1, sticky - 1, 3, 3), Color.Red);
                    }
                }
                else
                {
                    int touchx = (int)Math.Round(((double)(oldtouch & 0xFFF) / 0xFFF) * 319);
                    int touchy = (int)Math.Round(((double)((oldtouch >> 0x0C) & 0xFFF) / 0xFFF) * 239);
                    spriteBatch.Draw(Cursor, new Rectangle(touchx - 1, touchy - 1, 3, 3), Color.Green);
                }

                int cpadx = (int)Math.Round(((double)(oldcpad & 0xFFF) / 0xFFF) * 319);
                int cpady = (int)Math.Round(239 - (((double)((oldcpad >> 0x0C) & 0xFFF) / 0xFFF) * 239));
                spriteBatch.Draw(Cursor, new Rectangle(cpadx - 1, cpady - 1, 3, 3), Color.Blue);
            }
        }

        private void ShowIPInput()
        {
            DrawString(0, 0, "IP Address: " + IPAddress, Color.White);
        }

        private void ShowKeyboardInput()
        {
            DrawString(68, 28, "3DS        : Keyboard", Color.White);

            DrawString(68, 44, "DPad Up    : " + KeyboardInput[6], Color.White);
            DrawString(68, 52, "DPad Down  : " + KeyboardInput[7], Color.White);
            DrawString(68, 60, "DPad Left  : " + KeyboardInput[5], Color.White);
            DrawString(68, 68, "DPad Right : " + KeyboardInput[4], Color.White);

            DrawString(68, 84, "CPad Up    : " + KeyboardInput[14], Color.White);
            DrawString(68, 92, "CPad Down  : " + KeyboardInput[15], Color.White);
            DrawString(68, 100, "CPad Left  : " + KeyboardInput[13], Color.White);
            DrawString(68, 108, "CPad Right : " + KeyboardInput[12], Color.White);


            DrawString(68, 124, "A          : " + KeyboardInput[0], Color.White);
            DrawString(68, 132, "B          : " + KeyboardInput[1], Color.White);
            DrawString(68, 140, "Y          : " + KeyboardInput[11], Color.White);
            DrawString(68, 148, "X          : " + KeyboardInput[10], Color.White);

            DrawString(68, 164, "L          : " + KeyboardInput[9], Color.White);
            DrawString(68, 172, "R          : " + KeyboardInput[8], Color.White);
            DrawString(68, 180, "Start      : " + KeyboardInput[3], Color.White);
            DrawString(68, 188, "Select     : " + KeyboardInput[2], Color.White);
        }

        private void ShowGamePadInput()
        {
            DrawString(68, 28, "Controller : 3DS", Color.White);
            DrawString(68, 44, "DPad Up    : " + GetButtonNameFromValue(GamePadInput[6]), Color.White);
            DrawString(68, 52, "DPad Down  : " + GetButtonNameFromValue(GamePadInput[7]), Color.White);
            DrawString(68, 60, "DPad Left  : " + GetButtonNameFromValue(GamePadInput[5]), Color.White);
            DrawString(68, 68, "DPad Right : " + GetButtonNameFromValue(GamePadInput[4]), Color.White);

            DrawString(68, 84, "Y Axis+    : CPad Up", Color.Gray);
            DrawString(68, 92, "Y Axis-    : CPad Down", Color.Gray);
            DrawString(68, 100, "X Axis+    : CPad Left", Color.Gray);
            DrawString(68, 108, "X Axis-    : CPad Right", Color.Gray);


            DrawString(68, 124, "B          : " + GetButtonNameFromValue(GamePadInput[0]), Color.White);
            DrawString(68, 132, "A          : " + GetButtonNameFromValue(GamePadInput[1]), Color.White);
            DrawString(68, 140, "X          : " + GetButtonNameFromValue(GamePadInput[11]), Color.White);
            DrawString(68, 148, "Y          : " + GetButtonNameFromValue(GamePadInput[10]), Color.White);

            DrawString(68, 164, "LB         : " + GetButtonNameFromValue(GamePadInput[9]), Color.White);
            DrawString(68, 172, "RB         : " + GetButtonNameFromValue(GamePadInput[8]), Color.White);
            DrawString(68, 180, "Start      : " + GetButtonNameFromValue(GamePadInput[3]), Color.White);
            DrawString(68, 188, "Back       : " + GetButtonNameFromValue(GamePadInput[2]), Color.White);
        }

        private void ShowMacroMenu()
        {
            spriteBatch.Draw(Background, new Rectangle(0, 0, 320, 240), Color.White);
            DrawString(50, 28, "Macro Menu", Color.White);
            DrawString(50, 44, "Left Trigger  : " + MacroInput[0].ToString("X8"), Color.White);
            DrawString(50, 52, "Right Trigger : " + MacroInput[1].ToString("X8"), Color.White);
            DrawString(50, 60, "L3            : " + MacroInput[2].ToString("X8"), Color.White);
            DrawString(50, 68, "R3            : " + MacroInput[3].ToString("X8"), Color.White);
            DrawString(50, 80, "Press Esc to Back", Color.White);
        }

        private void ShowTouchScreenPic()
        {
            spriteBatch.Draw(Background, new Rectangle(0, 0, 320, 240), Color.White);
            DrawString(10, 220, "Press Backspace to cancel", Color.White);
            int mousex = Mouse.GetState().Position.X;
            int mousey = Mouse.GetState().Position.Y;
            if (oldtouch == 0x2000000)
            {
                if ((GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.X == 0.0) && (GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.Y == 0.0))
                {
                    if (MouseInWindow(mousex, mousey))
                    {
                        spriteBatch.Draw(Cursor, new Rectangle(mousex - 1, mousey - 1, 3, 3), Color.Red);
                    }
                }
                else
                {
                    int stickx = (int)Math.Round(159.5 + (GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.X * 159.5));
                    int sticky = (int)Math.Round(119.5 - (GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.Y * 119.5));
                    spriteBatch.Draw(Cursor, new Rectangle(stickx - 1, sticky - 1, 3, 3), Color.Red);
                }
            }
            else
            {
                int touchx = (int)Math.Round(((double)(oldtouch & 0xFFF) / 0xFFF) * 319);
                int touchy = (int)Math.Round(((double)((oldtouch >> 0x0C) & 0xFFF) / 0xFFF) * 239);
                spriteBatch.Draw(Cursor, new Rectangle(touchx - 1, touchy - 1, 3, 3), Color.Green);
            }

            int cpadx = (int)Math.Round(((double)(oldcpad & 0xFFF) / 0xFFF) * 319);
            int cpady = (int)Math.Round(239 - (((double)((oldcpad >> 0x0C) & 0xFFF) / 0xFFF) * 239));
            spriteBatch.Draw(Cursor, new Rectangle(cpadx - 1, cpady - 1, 3, 3), Color.Blue);
        }

        private string GetButtonNameFromValue(uint value)
        {
            string result = "None";

            for (int i = 0; i < ButtonNames.Length; i++)
            {
                if ((value >> i) == 0x01)
                {
                    result = ButtonNames[i];
                    break;
                }
            }

            return result;
        }

        private string KeytoText(Keys key)
        {
            string result = "";

            switch (key)
            {
                case Keys.NumPad0:
                case Keys.NumPad1:
                case Keys.NumPad2:
                case Keys.NumPad3:
                case Keys.NumPad4:
                case Keys.NumPad5:
                case Keys.NumPad6:
                case Keys.NumPad7:
                case Keys.NumPad8:
                case Keys.NumPad9:
                    {
                        result = key.ToString().Substring(6);
                    }
                    break;

                case Keys.D0:
                case Keys.D1:
                case Keys.D2:
                case Keys.D3:
                case Keys.D4:
                case Keys.D5:
                case Keys.D6:
                case Keys.D7:
                case Keys.D8:
                case Keys.D9:
                    {
                        result = key.ToString().Substring(1);
                    }
                    break;

                case Keys.Decimal:
                case Keys.OemPeriod:
                    {
                        result = ".";
                    }
                    break;
            }
            return result;
        }

        private void DrawString(int X, int Y, string data, Color color)
        {
            int TexX = 0;
            int TexY = 0;
            for (int i = 0; i < data.Length; i++)
            {
                TexX = ((data[i]) & 0x0F) * 0x08;
                TexY = (((data[i]) & 0xF0) >> 0x04) * 0x08;
                spriteBatch.Draw(Font, new Rectangle(X, Y, 8, 8), new Rectangle(TexX, TexY, 8, 8), color);
                X += 8;
            }
        }

        private bool MouseInWindow(int x, int y)
        {
            bool result = false;
            if (((x >= 0) && (x < 320)) && ((y >= 0) && (y < 240)))
            {
                result = true;
            }
            return result;
        }

        private void ClampValues(ref int x, ref int y)
        {
            if (x < 0)
            {
                x = 0;
            }

            if (y < 0)
            {
                y = 0;
            }

            if (x > 319)
            {
                x = 319;
            }

            if (y > 239)
            {
                y = 239;
            }
        }

        private void TouchInput(ref uint value, ref uint mouseclick, bool cpad)
        {
            int X = Mouse.GetState().Position.X;
            int Y = Mouse.GetState().Position.Y;

            if (mouseclick == 0x00)
            {
                if (MouseInWindow(X, Y))
                {
                    mouseclick = 0x01;
                }
                else
                {
                    mouseclick = 0x02;
                }
            }

            if (mouseclick == 0x01)
            {
                ClampValues(ref X, ref Y);
                X = (int)Math.Round(((double)X / 319) * 4095);
                if (cpad)
                {
                    Y = (int)(4095 - Math.Round(((double)Y / 239) * 4095));
                }
                else
                {
                    Y = (int)Math.Round(((double)Y / 239) * 4095);
                }
                value = (uint)X + ((uint)Y << 0x0C) + 0x01000000;
            }
        }

        private void RSticktoDpad()
        {
            Vector2 direction = GamePad.GetState(PlayerIndex.One).ThumbSticks.Right;

            float absX = Math.Abs(direction.X);
            float absY = Math.Abs(direction.Y);

            if (absX > absY && absX > thumbstickTolerance)
            {
                if (direction.X > 0) //right
                {
                    if (direction.Y > 0) //up
                    {
                        if ((newbuttons & GamePadInput[6]) != GamePadInput[6])
                        {
                            newbuttons += GamePadInput[6];
                        }
                    }

                    if ((newbuttons & GamePadInput[4]) != GamePadInput[4])
                    {
                        newbuttons += GamePadInput[4];
                    }

                }
                else //left
                {
                    if (direction.Y < 0) //down
                    {
                        if ((newbuttons & GamePadInput[7]) != GamePadInput[7])
                        {
                            newbuttons += GamePadInput[7];
                        }
                    }
                    if ((newbuttons & GamePadInput[5]) != GamePadInput[5])
                    {
                        newbuttons += GamePadInput[5];
                    }
                }
            }
            else if (absX < absY && absY > thumbstickTolerance)
            {
                if (direction.Y > 0) //up
                {
                    if (direction.X < 0) //left
                    {
                        if ((newbuttons & GamePadInput[5]) != GamePadInput[5])
                        {
                            newbuttons += GamePadInput[5];
                        }
                    }
                    if ((newbuttons & GamePadInput[6]) != GamePadInput[6])
                    {
                        newbuttons += GamePadInput[6];
                    }
                }
                else //down
                {
                    if (direction.X > 0) //right
                    {
                        if ((newbuttons & GamePadInput[4]) != GamePadInput[4])
                        {
                            newbuttons += GamePadInput[4];
                        }
                    }
                    if ((newbuttons & GamePadInput[7]) != GamePadInput[7])
                    {
                        newbuttons += GamePadInput[7];
                    }
                }
            }
        }

        private void SendInput()
        {
            if ((newbuttons != oldbuttons) || (newtouch != oldtouch) || (newcpad != oldcpad))
            {
                oldbuttons = newbuttons;
                oldtouch = newtouch;
                oldcpad = newcpad;

                //Buttons
                data[0x00] = (byte)(oldbuttons & 0xFF);
                data[0x01] = (byte)((oldbuttons >> 0x08) & 0xFF);
                data[0x02] = (byte)((oldbuttons >> 0x10) & 0xFF);
                data[0x03] = (byte)((oldbuttons >> 0x18) & 0xFF);

                //Touch
                data[0x04] = (byte)(oldtouch & 0xFF);
                data[0x05] = (byte)((oldtouch >> 0x08) & 0xFF);
                data[0x06] = (byte)((oldtouch >> 0x10) & 0xFF);
                data[0x07] = (byte)((oldtouch >> 0x18) & 0xFF);

                //CPad
                data[0x08] = (byte)(oldcpad & 0xFF);
                data[0x09] = (byte)((oldcpad >> 0x08) & 0xFF);
                data[0x0A] = (byte)((oldcpad >> 0x10) & 0xFF);
                data[0x0B] = (byte)((oldcpad >> 0x18) & 0xFF);

                CheckConnection();
                if (Program.Connected)
                {
                    Program.scriptHelper.write(0x10DF20, data, 0x10);
                }
            }
        }
    }
}
