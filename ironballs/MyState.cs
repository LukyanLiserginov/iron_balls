using ImGuiNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Urho3DNet;

namespace ironballs
{
    public partial class MyState : ApplicationState
    {
        protected readonly Scene _scene;
        private readonly UrhoPluginApplication _app;
        private readonly Node _cameraNode;
        private readonly Node _arrowNode;
        private readonly Node _selectNode;
        private readonly Camera _camera;
        private readonly Viewport _viewport;
        private readonly InputMap _inputMap;
        private Random _random = new Random();
        private Node _selectedBall;
        private Node _lastBall;
        private Node _cube;
        private RigidBody _cubeRb;
        private Node _trajectory = null;
        private CustomGeometry _customGeometry = null;

        private Vector3 _curVelocity;
        private float _inertia = 0.8f;
        private float _speed = 0.02f;
        private int _hitTimer = 0;
        private float _ballStr = 0.5f;
        private bool _strUp = true;

        private Team team1 = new Team("team1","Игрок 1");
        private Team team2 = new Team("team2", "Игрок 2");
        private Team team3 = new Team("team3", "Игрок 3");
        private Team team4 = new Team("team4", "Игрок 4");
        private Team _activeTeam = null;
        private bool _nowPlayer = true;

        private Vector2 _touchBegin = new Vector2 (0, 0);
        private Vector3 _touchDebug = new Vector3 (0, 0, 0);

        public MyState(UrhoPluginApplication app) : base(app.Context)
        {
            MouseMode = MouseMode.MmFree;
            IsMouseVisible = true;

            _app = app;
            _scene = Context.CreateObject<Scene>();
            _scene.Load("Scenes/Scene.scene");
            _cameraNode = _scene.FindChild("CameraNode", true);
            _arrowNode = _scene.FindChild("ArrowNode", true);
            _selectNode = _scene.FindChild("SelectNode", true);
            _selectedBall = _scene.FindChild("ball1", true);
            _lastBall = _selectedBall;
            _cube = _scene.FindChild("Cube", true);
            _cubeRb = _cube.FindComponent<RigidBody>();
            var temp = _scene.GetChildren();
            foreach (var child in temp)
            {
                if (child.Name == "ball1") team1.Balls.Add(child);
                else if (child.Name == "ball2") team2.Balls.Add(child);
                else if (child.Name == "ball3") team3.Balls.Add(child);
                else if (child.Name == "ball4") team4.Balls.Add(child);
            }
            _camera = _cameraNode.GetComponent<Camera>();
            _viewport = Context.CreateObject<Viewport>();
            _viewport.Scene = _scene;
            _viewport.Camera = _camera;
            SetViewport(0, _viewport);
            _inputMap = Context.ResourceCache.GetResource<InputMap>("Input/MoveAndOrbit.inputmap");
            SetupPlayers();
            if (!team1.IsPlayer) _nowPlayer = false;

            Material transparentMaterial = new Material(Context);
            transparentMaterial.CullMode = CullMode.CullNone;
            transparentMaterial.NumTechniques = 1;
            transparentMaterial.SetTechnique(0, GetSubsystem<ResourceCache>().GetResource<Technique>("Techniques/NoTextureUnlitAlpha.xml"));
            transparentMaterial.SetShaderParameter("MatDiffColor", Color.White);
            transparentMaterial.VertexShaderDefines = "VERTEXCOLOR";
            transparentMaterial.PixelShaderDefines = "VERTEXCOLOR";

            _activeTeam = team1;

            _trajectory = _scene.CreateChild("Traectory");
            _customGeometry = _trajectory.CreateComponent<CustomGeometry>();
            _customGeometry.SetMaterial(transparentMaterial);
        }


        public void SetupPlayers()
        {
            var maxPlayers = MySetup.Players;
            for (int i = 1; i <= maxPlayers; i++)
            {
                switch (i)
                {
                    case 1:
                        team1.IsPlayer = MySetup.Players >= i;
                        break;
                    case 2:
                        team2.IsPlayer = MySetup.Players >= i;
                        break;
                    case 3:
                        team3.IsPlayer = MySetup.Players >= i;
                        break;
                    case 4:
                        team4.IsPlayer = MySetup.Players >= i;
                        break;
                }
            }
        }

        public override void Update(float timeStep)
        {
            if (_inputMap.Evaluate("In") > 0.5f) _cameraNode.Position += _cameraNode.Direction * 0.01f;
            if (_inputMap.Evaluate("Out") > 0.5f) _cameraNode.Position += _cameraNode.Direction * -0.01f;
            if (_selectedBall!=null)
            {
                _selectNode.Position = new Vector3(_selectedBall.Position.X, 0.6f, _selectedBall.Position.Z);
            }

            if (_hitTimer > 0) 
            {
                HideTrajectory();
                _hitTimer--;
                if (_hitTimer == 2)
                {
                    RemoveBalls();
                    NextPlayer();
                }
            } 
            else if (_nowPlayer)
            {
                var left = _inputMap.Evaluate("Left");
                var right = _inputMap.Evaluate("Right");
                var forward = _inputMap.Evaluate("Forward");
                var back = _inputMap.Evaluate("Back");

                var velocity = new Vector3(forward - back, 0, left - right);
                velocity.Normalize();
                _curVelocity = _inertia * _curVelocity + (1 - _inertia) * velocity * _speed;
                var newPosition = _arrowNode.Position + _curVelocity;
                _arrowNode.Position = newPosition;
                ArrowDir();
                ShowTrajectory();

                if (_inputMap.Evaluate("Use") > 0.5f && _hitTimer < 1 && _strUp) _ballStr += 0.01f;
                if (_inputMap.Evaluate("Use") > 0.5f && _hitTimer < 1 && !_strUp) _ballStr += -0.01f;
                else if (_inputMap.Evaluate("Use") < 0.5f && _hitTimer < 1 && _ballStr > 0.5f)
                {
                    _strUp = true;
                    Hit(_ballStr);
                }

                if (_strUp && _ballStr > 2.5f) _strUp = false;
                else if (!_strUp && _ballStr < 0.5f) _strUp = true;

            }
            else
            {
                Hit();
            }


            //UI
            ImGui.Begin("Info");
            ImGui.Text(_activeTeam.Description);
            ImGui.ProgressBar((_ballStr-0.5f)/2);
            //ImGui.Text(_touchBegin.ToString());
            //ImGui.Text(_touchDebug.ToString());
            ImGui.End();
        }

        public void NextPlayer()
        {
            int c = 0;
            Team t = null;
            if (team1.Balls.Count > 0) 
            {
                c++;
                t = team1;
            }
            if (team2.Balls.Count > 0) { c++; t = team2; }
            if (team3.Balls.Count > 0) { c++; t = team3; }
            if (team4.Balls.Count > 0) {c++; t = team4; }
            if (c < 2) 
            { 
                System.Console.WriteLine(t.ToString() + " is a winner"); 
                _app.HandleBackKey();
                return;
            }


            if (_cubeRb.Mass < 1 && team1.Balls.Count + team2.Balls.Count + team3.Balls.Count + team4.Balls.Count < 8)
                _cubeRb.Mass = 2;

            //t1
            if (_activeTeam == team1)
            {
                _activeTeam = team2;
                if (team2.Balls.Count > 0)
                {
                    _random = new Random();
                    var r = _random.Next(0,team2.Balls.Count);
                    _selectedBall = team2.Balls[r];
                    if (team2.IsPlayer)
                    {
                        _nowPlayer = true;
                        _arrowNode.Position = new Vector3(0, 0.25f, 0);
                    }
                    else
                    {
                        _nowPlayer = false;
                        AiArrowPos();
                    }
                }
                else NextPlayer();
            }
            //t2
            else if (_activeTeam == team2)
            {
                _activeTeam = team3;
                if (team3.Balls.Count > 0)
                {
                    _random = new Random();
                    var r = _random.Next(0, team3.Balls.Count);
                    _selectedBall = team3.Balls[r];
                    if (team3.IsPlayer)
                    {
                        _nowPlayer = true;
                        _arrowNode.Position = new Vector3(0, 0.25f, 0);
                    }
                    else
                    {
                        _nowPlayer = false;
                        AiRandomPos();
                    }
                }
                else NextPlayer();
            }
            //t3
            else if (_activeTeam == team3)
            {
                _activeTeam = team4;
                if (team4.Balls.Count > 0)
                {
                    _random = new Random();
                    var r = _random.Next(0, team4.Balls.Count);
                    _selectedBall = team4.Balls[r];
                    if (team4.IsPlayer)
                    {
                        _nowPlayer = true;
                        _arrowNode.Position = new Vector3(0, 0.25f, 0);
                    }
                    else
                    {
                        _nowPlayer = false;
                        AiDsPos();
                    }
                }
                else NextPlayer();
            }
            //t4
            else if (_activeTeam == team4)
            {
                _activeTeam = team1;
                if (team1.Balls.Count > 0)
                {
                    var r = _random.Next(0, team1.Balls.Count);
                    _selectedBall = team1.Balls[r];
                    if (team1.IsPlayer)
                    {
                        _nowPlayer = true;
                        _arrowNode.Position = new Vector3(0, 0.25f, 0);
                    }
                    else
                    {
                        _nowPlayer = false;
                        AiNullPos();
                    }
                }
                else NextPlayer();
            }
        }

        public void AiArrowPos()
        {
            var v = new Vector3(_selectedBall.Position.X * 2, 0.25f, _selectedBall.Position.Z * 2);
            _arrowNode.Position = v;
            ArrowDir();
        }
        public void AiRandomPos()
        {
            var v = new Vector3(_random.Next(-4,4), 0.25f, _random.Next(-4, 4));
            _arrowNode.Position = v;
            ArrowDir();
        }
        public void AiNullPos()
        {
            _arrowNode.Position = new Vector3(0, 0.25f, 0);
            ArrowDir();
        }
        public void AiDsPos()
        {
            Vector3 ballPos = _selectedBall.Position;
            Vector3 toCenter = -ballPos;  // Вектор от шара к центру
            float distanceToCenter = toCenter.Length;

            // Радиусы "опасных зон" (лунка + радиус шара)
            float centerHoleSafeRadius = 0.1f + 0.075f;  // 0.175f
            float cornerHoleSafeRadius = 0.2f + 0.075f;  // 0.275f

            // Проверяем, близко ли шар к центру или углам
            bool isNearCenter = distanceToCenter < centerHoleSafeRadius;
            bool isNearCorner = Math.Abs(ballPos.X) > 1.5f - cornerHoleSafeRadius &&
                                Math.Abs(ballPos.Z) > 1.5f - cornerHoleSafeRadius;

            Vector3 pushDirection;

            if (isNearCenter)
            {
                // Касательная к центральной лунке (перпендикулярно вектору к центру)
                pushDirection = new Vector3(-toCenter.Z, 0, toCenter.X);
                pushDirection.Normalize();  // Нормализуем вектор
            }
            else if (isNearCorner)
            {
                // Толкаем вдоль ближайшей стенки (по оси X или Z)
                if (Math.Abs(ballPos.X) > Math.Abs(ballPos.Z))
                {
                    pushDirection = new Vector3(0, 0, Math.Sign(ballPos.Z));
                }
                else
                {
                    pushDirection = new Vector3(Math.Sign(ballPos.X), 0, 0);
                }
                // Нормализация не нужна, так как направление уже единичное (0,0,±1) или (±1,0,0)
            }
            else
            {
                // Толкаем "почти в центр", но с минимальным отклонением (касательная к малой окружности)
                Vector3 tangent = new Vector3(-toCenter.Z, 0, toCenter.X);
                tangent.Normalize();  // Нормализуем касательную

                // Комбинируем направление к центру и касательную (с коэффициентом отклонения 0.1f)
                pushDirection = new Vector3(
                    toCenter.X / distanceToCenter + tangent.X * 0.1f,
                    0,
                    toCenter.Z / distanceToCenter + tangent.Z * 0.1f
                );
                pushDirection.Normalize();  // Нормализуем итоговый вектор
            }

            // Позиция стрелки (на расстоянии 0.3f от шара)
            var v = ballPos + pushDirection * 0.3f;
            v.Y = 0.25f;
            _arrowNode.Position = v;  
            ArrowDir();
        }

        public void Hit(float str = 1.25f)
        {
            var body = _selectedBall.GetComponent<RigidBody>();
            body.ApplyImpulse(( _selectedBall.Position - _arrowNode.Position).Normalized * str);
            _lastBall = _selectedBall;
            _hitTimer = 300;
            _ballStr = 0.5f;
        }

        public void ShowTrajectory()
        {
            _trajectory.IsEnabled = true;
            var e = EndPosition();
            _customGeometry.BeginGeometry(0, PrimitiveType.LineStrip);
            GeometryBuilder builder = new GeometryBuilder(_customGeometry);
            SimpleVertex[] frame = new SimpleVertex[4];
            frame[0] = new SimpleVertex(new Vector3(_selectedBall.Position.X, 0.35f, _selectedBall.Position.Z), Color.Red);
            frame[1] = new SimpleVertex(new Vector3(e.X, 0.35f, e.Z), Color.Red);
            frame[2] = new SimpleVertex(new Vector3(e.X, 0.3f, e.Z), Color.Red);
            frame[3] = new SimpleVertex(new Vector3(_selectedBall.Position.X, 0.3f, _selectedBall.Position.Z), Color.Red);
            builder.BuildSolidQuad(frame);

            _customGeometry.Commit();
        }

        public void HideTrajectory()
        {
            _trajectory.IsEnabled = false;
        }

        public Vector3 EndPosition()
        {
            try
            {
                var _raycastResult = new PhysicsRaycastResult();
                var world = _scene.GetComponent<PhysicsWorld>();
                world.RaycastSingle(_raycastResult, new Ray(_selectedBall.WorldPosition, - _arrowNode.WorldDirection),
                    10f);
                var selectedNode = _raycastResult.Body?.Node;
                return _raycastResult.Position;

            }
            catch 
            {
                return Vector3.Zero;
            }
        }

        public void RemoveBalls()
        {
            foreach (var ball in team1.Balls.ToList())
            {
                if (ball.Position.Y < -2)
                {
                    team1.Balls.Remove(ball);
                    ball.Remove();
                }
            }
            foreach (var ball in team2.Balls.ToList())
            {
                if (ball.Position.Y < -2)
                {
                    team2.Balls.Remove(ball);
                    ball.Remove();
                }
            }
            foreach (var ball in team3.Balls.ToList())
            {
                if (ball.Position.Y < -2)
                {
                    team3.Balls.Remove(ball);
                    ball.Remove();
                }
            }
            foreach (var ball in team4.Balls.ToList())
            {
                if (ball.Position.Y < -2)
                {
                    team4.Balls.Remove(ball);
                    ball.Remove();
                }
            }
        }

        public void ArrowDir()
        {
            _arrowNode.Direction = new Vector3 (_arrowNode.Position.X - _selectedBall.Position.X, 0, _arrowNode.Position.Z - _selectedBall.Position.Z).Normalized;
        }

        private void HandleTouchBegin(VariantMap args)
        {
            _touchBegin = new Vector2(args[E.TouchBegin.X].Int, args[E.TouchBegin.Y].Int);
        }


        private void HandleTouchEnd(VariantMap args)
        {
            _touchBegin = Vector2.ZERO;
            _touchDebug = Vector3.Zero;
            if (_nowPlayer && args[E.TouchEnd.Y].Int < 200) Hit();
        }

        private void HandleTouchMove(VariantMap args)
        {
            
            if (_nowPlayer) 
            {
                _touchDebug = new Vector3(_touchBegin.Y - args[ E.TouchMove.Y].Int, 0,_touchBegin.X -args[E.TouchMove.X].Int);
                _arrowNode.Position += _touchDebug*0.000005f;
            }
        }

        public override void Activate(StringVariantMap bundle)
        {
            SubscribeToEvent(E.KeyUp, HandleKeyUp);
            SubscribeToEvent(E.TouchBegin, HandleTouchBegin);
            SubscribeToEvent(E.TouchEnd, HandleTouchEnd);
            SubscribeToEvent(E.TouchMove, HandleTouchMove);
            _scene.IsUpdateEnabled = true;

            base.Activate(bundle);
        }

        public override void Deactivate()
        {
            _scene.IsUpdateEnabled = false;
            UnsubscribeFromEvent(E.TouchBegin);
            UnsubscribeFromEvent(E.TouchEnd);
            UnsubscribeFromEvent(E.TouchMove);
            base.Deactivate();
        }

        protected override void Dispose(bool disposing)
        {
            _scene?.Dispose();

            base.Dispose(disposing);
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
