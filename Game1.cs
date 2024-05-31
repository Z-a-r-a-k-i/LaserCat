using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace LaserCat
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _collisionMap;
        private List<Texture2D> _backgroundMapFrames;
        private Texture2D _collisionTexture;
        private Texture2D _dieScreen;
        private SpriteFont _font;
        private Player _player;
        private List<Enemy> _enemies;
        Color[,] _collisionMapData;
        private Camera _camera;
        private Song _backgroundMusic;

        private int _currentBackgroundFrame;
        private float _backgroundFrameTime;
        private const float BackgroundFrameDuration = 0.08f; // Duration for each frame in seconds

        private List<Texture2D> _youWonFrames;
        private int _currentYouWonFrame;
        private float _youWonFrameTime;
        private const float YouWonFrameDuration = 0.5f; // Duration for each frame in seconds

        private TimeSpan _elapsedTime;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.ApplyChanges();

            // init camera
            _camera = new Camera(GraphicsDevice.Viewport, _collisionMap.Width);

            // actors
            _player = new Player(new Vector2(75, 500));
            // _player = new Player(new Vector2(3555, 300)); // this is if you want to start in the end of the level
            _enemies = new List<Enemy>
            {
                new Enemy(710f, 500f, 610f),
                new Enemy(566f, 790f, 900f),
                new Enemy(782f, 1075f, 1190f),
                new Enemy(347f, 1725f, 1840f),
                new Enemy(638f, 2591f, 2700f)
            };

            // cache collision map data
            Color[] rawData = new Color[_collisionMap.Width * _collisionMap.Height];
            _collisionMap.GetData(rawData);

            _collisionMapData = new Color[_collisionMap.Width, _collisionMap.Height];
            for (int y = 0; y < _collisionMap.Height; y++)
            {
                for (int x = 0; x < _collisionMap.Width; x++)
                {
                    _collisionMapData[x, y] = rawData[x + y * _collisionMap.Width];
                }
            }

            // Initialize animation variables
            _currentBackgroundFrame = 0;
            _backgroundFrameTime = 0f;

            // Initialize timer
            _elapsedTime = TimeSpan.Zero;

            // Start the music
            MediaPlayer.Play(_backgroundMusic);
        }

        protected override void LoadContent()
        {
            // sprites
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _collisionMap = Content.Load<Texture2D>("collision_map_01");
            _backgroundMapFrames = new List<Texture2D>
            {
                Content.Load<Texture2D>("background/background_00000"),
                Content.Load<Texture2D>("background/background_00001"),
                Content.Load<Texture2D>("background/background_00002"),
                Content.Load<Texture2D>("background/background_00003"),
                Content.Load<Texture2D>("background/background_00004"),
                Content.Load<Texture2D>("background/background_00005"),
                Content.Load<Texture2D>("background/background_00006"),
                Content.Load<Texture2D>("background/background_00007"),
                Content.Load<Texture2D>("background/background_00008"),
                Content.Load<Texture2D>("background/background_00009"),
                Content.Load<Texture2D>("background/background_00010"),
                Content.Load<Texture2D>("background/background_00011")
            };

            // misc
            _font = Content.Load<SpriteFont>("Score");
            _backgroundMusic = Content.Load<Song>("music");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.2f;
            _dieScreen = Content.Load<Texture2D>("die_screen_00000");
            _youWonFrames = new List<Texture2D>
            {
                Content.Load<Texture2D>("win_screen_00000"),
                Content.Load<Texture2D>("win_screen_00001")
            };
            _currentYouWonFrame = 0;
            _youWonFrameTime = 0f;

            // player
            Player.WalkFrames = new List<Texture2D>
            {
                Content.Load<Texture2D>("cat/WalkCycle__00000"),
                Content.Load<Texture2D>("cat/WalkCycle__00001"),
                Content.Load<Texture2D>("cat/WalkCycle__00002"),
                Content.Load<Texture2D>("cat/WalkCycle__00003"),
                Content.Load<Texture2D>("cat/WalkCycle__00004")
            };
            Player.JumpStartFrames = new List<Texture2D>
            {
                Content.Load<Texture2D>("cat/JumpStart__00001"),
                Content.Load<Texture2D>("cat/JumpStart__00002"),
            };
            Player.JumpLandFrames = new List<Texture2D>
            {
                Content.Load<Texture2D>("cat/JumpLanding__00000"),
                Content.Load<Texture2D>("cat/JumpLanding__00001"),
                Content.Load<Texture2D>("cat/JumpLanding__00002")
            };
            Player.IdleFrame = Content.Load<Texture2D>("cat/Idle_00000");
            Player.JumpFlyFrame = Content.Load<Texture2D>("cat/JumpFly__00000");

            Player.DyingFX = Content.Load<SoundEffect>("sounds/player_dead").CreateInstance();
            Player.DyingFX.Volume = 0.3f;
            Player.JumpFX = Content.Load<SoundEffect>("sounds/jump").CreateInstance();
            Player.JumpFX.Volume = 0.3f;
            Player.WinningFX = Content.Load<SoundEffect>("sounds/winning").CreateInstance();
            Player.WinningFX.Volume = 0.3f;

            // laser
            Laser.ShootingFX = Content.Load<SoundEffect>("sounds/laser").CreateInstance();
            Laser.ShootingFX.Volume = 0.3f;
            Laser.ShootingFX.Pitch = 0.1f;
            Laser.Texture = Content.Load<Texture2D>("laser");

            // Enemy
            Enemy.Texture = Content.Load<Texture2D>("penguin");
            Enemy.DyingFX = Content.Load<SoundEffect>("sounds/enemy_dead").CreateInstance();
            Enemy.DyingFX.Volume = 0.3f;

            // debug stuff
            _collisionTexture = new Texture2D(GraphicsDevice, 1, 1);
            _collisionTexture.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.R))
            {
                Initialize();
            }

            if (!_player.Dead && !_player.Won)
            {
                _player.Update(gameTime, _collisionMapData, _enemies);
                foreach (Enemy enemy in _enemies)
                {
                    enemy.Update(gameTime);
                }
                _camera.Update(_player.Position);

                // Update the timer
                _elapsedTime += gameTime.ElapsedGameTime;
            }
            else if (_player.Dead)
            {
                _player.HandleDeath(gameTime);
            }

            // Update background frame
            _backgroundFrameTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_backgroundFrameTime >= BackgroundFrameDuration)
            {
                _currentBackgroundFrame = (_currentBackgroundFrame + 1) % _backgroundMapFrames.Count;
                _backgroundFrameTime = 0f;
            }

            if (_player.Won)
            {
                _youWonFrameTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_youWonFrameTime >= YouWonFrameDuration)
                {
                    _currentYouWonFrame = (_currentYouWonFrame + 1) % _youWonFrames.Count;
                    _youWonFrameTime = 0f;
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _spriteBatch.Begin(transformMatrix: _camera.Transform);

            // _spriteBatch.Draw(_collisionMap, new Rectangle(0, 0, 3840, 1080), Color.White);
            _spriteBatch.Draw(_backgroundMapFrames[_currentBackgroundFrame], new Rectangle(0, 0, 3840, 1080), Color.White);


            _player.Draw(_spriteBatch);
            // _player.DrawCollisionRectangle(_spriteBatch, _collisionTexture);

            foreach (Enemy enemy in _enemies)
            {
                enemy.Draw(_spriteBatch);
            }

            // Draw the timer
            string timerText = _elapsedTime.ToString(@"mm\:ss");
            Vector2 timerPosition = new Vector2(
                _camera.Position.X + _graphics.PreferredBackBufferWidth - 200,
                _camera.Position.Y + 20);
            _spriteBatch.DrawString(_font, timerText, timerPosition, Color.White);

            if (_player.Dead)
            {
                _spriteBatch.Draw(_dieScreen, new Rectangle((int)_camera.Position.X, (int)_camera.Position.Y, 1920, 1080), Color.White);
            }
            else if (_player.Won)
            {
                _spriteBatch.Draw(_youWonFrames[_currentYouWonFrame], new Rectangle((int)_camera.Position.X, (int)_camera.Position.Y, 1920, 1080), Color.White);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }

    public class Camera
    {
        public Matrix Transform { get; private set; }
        public Vector2 Position;
        private int _viewportWidth;
        private int _worldWidth;

        public Camera(Viewport viewport, int worldWidth)
        {
            _viewportWidth = viewport.Width;
            _worldWidth = worldWidth;
        }

        public void Update(Vector2 playerPosition)
        {
            Position.X = playerPosition.X - (_viewportWidth / 2);
            Position.X = MathHelper.Clamp(Position.X, 0, _worldWidth - _viewportWidth);
            Transform = Matrix.CreateTranslation(new Vector3(-Position, 0));
        }
    }
}

