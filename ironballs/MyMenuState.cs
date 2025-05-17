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
        private readonly Node _textNode;
        private readonly Camera _camera;
        private readonly Viewport _viewport;
        private readonly InputMap _inputMap;

        private bool _more = false;
        private bool _less = false;
        private bool _game = false;

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
            _textNode = _scene.FindChild("Ground Plane", true).FindChild("PlayersText", true);
        }

        

        public override void Update(float timeStep)
        {
            var left = _inputMap.Evaluate("Left") > 0.5f;
            if (left) 
            {
                if (left != _less)
                {
                    MySetup.Players--;
                    
                }
            }
            _less = left;
            var right = _inputMap.Evaluate("Right") > 0.5f;
            if (right)
            {
                if (right != _more)
                {
                    MySetup.Players++;
                }
            }
            _more = right;
            _textNode.FindComponent<Text3D>().Text = "Players: <" + MySetup.Players + ">";

            var game = _inputMap.Evaluate("Use") > 0.5f;
            if (game)
            {
                if (game != _game)
                {
                    _game = game;
                    _app.ToNewGame();
                }
            }
            if (_inputMap.Evaluate("Back") > 0.5f)
            {
                _app.Quit();
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
