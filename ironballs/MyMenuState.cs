using System;
using System.Collections.Generic;
using System.Text;
using Urho3DNet;


namespace ironballs
{
    internal partial class MyMenuState : ApplicationState
    {
        protected readonly Scene _scene;
        private readonly UrhoPluginApplication _app;
        private readonly Node _cameraNode;
        private readonly Node _playersTextNode;
        private readonly Node _gameTextNode;
        private readonly Node _arrowNode;
        private readonly Camera _camera;
        private readonly Viewport _viewport;
        private readonly InputMap _inputMap;

        private bool _more = false;
        private bool _less = false;
        private bool _up = false;
        private bool _down = false;
        private bool _game = false;
        private bool _settings = false;
        private float[] _arrowPosY = new float[] { 2.05f, 0.65f, -0.75f, -2.15f };
        private int _arrowP = 0;

        public MyMenuState(UrhoPluginApplication app) : base(app.Context)
        {
            MouseMode = MouseMode.MmFree;
            IsMouseVisible = true;

            _app = app;
            _scene = Context.CreateObject<Scene>();
            _scene.Load("Scenes/MyMenu.scene");
            _cameraNode = _scene.FindChild("CameraNode", true);
            _camera = _cameraNode.GetComponent<Camera>();
            _viewport = Context.CreateObject<Viewport>();
            _viewport.Scene = _scene;
            _viewport.Camera = _camera;
            SetViewport(0, _viewport);
            _inputMap = Context.ResourceCache.GetResource<InputMap>("Input/MoveAndOrbit.inputmap");
            _gameTextNode = _scene.FindChild("Ground Plane", true).FindChild("GameMode", true);
            _playersTextNode = _scene.FindChild("Ground Plane", true).FindChild("PlayersText", true);
            _arrowNode = _scene.FindChild("ArrowNode", true);
        }

        

        public override void Update(float timeStep)
        {
            var left = _inputMap.Evaluate("Left") > 0.5f;
            if (left) 
            {
                if (left != _less)
                {
                    if (_arrowP == 0)
                        MySetup.GameMode--;
                    else if (_arrowP == 1)
                        MySetup.Players--;

                }
            }
            _less = left;
            var right = _inputMap.Evaluate("Right") > 0.5f;
            if (right)
            {
                if (right != _more)
                {
                    if (_arrowP == 0)
                        MySetup.GameMode++;
                    else if (_arrowP == 1)
                        MySetup.Players++;
                }
            }
            _more = right;
            _gameTextNode.FindComponent<Text3D>().Text = "Game: <" + MySetup.GameMode + ">";
            _playersTextNode.FindComponent<Text3D>().Text = "Players: <" + MySetup.Players + ">";

            var up = _inputMap.Evaluate("Forward") > 0.5f;
            if (up)
            {
                if (up != _up)
                {
                    if (_arrowP > 0)
                    {
                        _arrowP--;
                        var pos = _arrowNode.Position;
                        pos.Y = _arrowPosY[_arrowP]; 
                        _arrowNode.Position = pos;
                    }
                    else
                    {
                        _arrowP = 3;
                        var pos = _arrowNode.Position;
                        pos.Y = _arrowPosY[_arrowP];
                        _arrowNode.Position = pos;
                    }
                        
                        
                }
            }
            _up = up;

            var down = _inputMap.Evaluate("Back") > 0.5f;
            if (down)
            {
                if (down != _down)
                {
                    if (_arrowP < 3)
                    {
                        _arrowP++;
                        var pos = _arrowNode.Position;
                        pos.Y = _arrowPosY[_arrowP];
                        _arrowNode.Position = pos;
                    }
                    else
                    {
                        _arrowP = 0;
                        var pos = _arrowNode.Position;
                        pos.Y = _arrowPosY[_arrowP];
                        _arrowNode.Position = pos;
                    }


                }
            }
            _down = down;

            var game = _inputMap.Evaluate("Use") > 0.5f;
            if (game)
            {
                if (game != _game && _arrowP == 3)
                {
                    _game = game;
                    _app.ToNewGame();
                }
            }
           

        }

        public override void Activate(StringVariantMap bundle)
        {
            SubscribeToEvent(E.KeyUp, HandleKeyUp);
            SubscribeToEvent(E.TouchEnd, HandleTouchEnd);
            _scene.IsUpdateEnabled = true;
            _game = false;

            base.Activate(bundle);
        }

        public override void Deactivate()
        {
            _scene.IsUpdateEnabled = false;
            UnsubscribeFromEvent(E.KeyUp);
            UnsubscribeFromEvent(E.TouchEnd);
            base.Deactivate();
        }

        protected override void Dispose(bool disposing)
        {
            _scene?.Dispose();

            base.Dispose(disposing);
        }

        private void HandleTouchEnd(VariantMap args)
        {
            if (args[E.TouchEnd.X].Int < 200) MySetup.Players--;
            else if (args[E.TouchEnd.X].Int > Graphics.Width - 200) MySetup.Players++;
            else if (args[E.TouchEnd.Y].Int < 200) _app.ToNewGame();
        }

        private void HandleKeyUp(VariantMap args)
        {
            var key = (Key)args[E.KeyUp.Key].Int;
            switch (key)
            {
                case Key.KeyEscape:
                case Key.KeyBackspace:
                    _app.HandleBackKey();
                    return;
            }
        }


    }
}
