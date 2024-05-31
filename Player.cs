using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace LaserCat
{
    public class Player
    {
        public static SoundEffectInstance WinningFX;
        public static SoundEffectInstance DyingFX;
        public static SoundEffectInstance JumpFX;
        public static List<Texture2D> WalkFrames;
        public static List<Texture2D> JumpStartFrames;
        public static List<Texture2D> JumpLandFrames;
        public static Texture2D JumpFlyFrame;
        public static Texture2D IdleFrame;



        const float Gravity = 1000f; // Gravity constant (pixels per second^2)
        const float Acceleration = 3000f; // Horizontal acceleration (pixels per second^2)
        const float Deceleration = 1500f; // Horizontal deceleration (pixels per second^2)
        const float MaxSpeed = 400f; // Maximum horizontal speed (pixels per second)
        const float JumpVelocity = -600f; // Initial jump velocity (pixels per second)
        const float CollisionWidth = 80f;
        const float CollisionHeight = 95f;
        const float CollisionOffsetX = 15f;
        const float CollisionOffsetY = 35f;

        public float Scale = 0.1f; // Default scale is 1 (original size)
        public Vector2 Position;
        public Vector2 Velocity;
        private bool isOnGround;
        private bool facingRight = true;
        public bool Dead = false;
        public bool Won = false;
        private bool deathHandled = false;
        private List<Laser> _lasers;
        private const float LaserSpeed = 500f;
        private const float LaserCooldown = 0.25f; // 1 laser every 0.25 seconds
        private float _laserCooldownRemaining;

        private int _currentFrame;
        private float _frameTime;
        private float _timeForCurrentFrame;
        private enum AnimationState { Idle, Walking, Jumping, Falling, Landing }
        private AnimationState _currentAnimationState = AnimationState.Idle;
        private int _currentJumpStartFrame;
        private int _currentJumpLandFrame;
        private float _jumpAnimationTime;

        public Player(Vector2 position)
        {
            Position = position;
            Velocity = Vector2.Zero;
            _lasers = new List<Laser>();
            _laserCooldownRemaining = 0f;

            _currentFrame = 0;
            _frameTime = 0.1f; // Time each frame is displayed (in seconds)
            _timeForCurrentFrame = 0f;
        }

        public void Update(GameTime gameTime, Color[,] collisionMapData, List<Enemy> enemies)
        {
            if (Dead || Won) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _laserCooldownRemaining -= deltaTime;

            // Handle player movement with arrow keys (horizontal movement with inertia)
            KeyboardState state = Keyboard.GetState();
            bool isMoving = false;
            if (state.IsKeyDown(Keys.Left))
            {
                Velocity.X -= Acceleration * deltaTime;
                if (Velocity.X < -MaxSpeed)
                    Velocity.X = -MaxSpeed;
                facingRight = false;
                isMoving = true;
            }
            else if (state.IsKeyDown(Keys.Right))
            {
                Velocity.X += Acceleration * deltaTime;
                if (Velocity.X > MaxSpeed)
                    Velocity.X = MaxSpeed;
                facingRight = true;
                isMoving = true;
            }
            else
            {
                // Apply deceleration when no input is given
                if (Velocity.X > 0)
                {
                    Velocity.X -= Deceleration * deltaTime;
                    if (Velocity.X < 0)
                        Velocity.X = 0;
                }
                else if (Velocity.X < 0)
                {
                    Velocity.X += Deceleration * deltaTime;
                    if (Velocity.X > 0)
                        Velocity.X = 0;
                }
            }

            // Handle jumping
            if (state.IsKeyDown(Keys.Up) && isOnGround)
            {
                Velocity.Y = JumpVelocity;
                isOnGround = false;
                JumpFX.Play();
                _currentAnimationState = AnimationState.Jumping;
                _currentJumpStartFrame = 0;
                _jumpAnimationTime = 0f;
            }

            // Apply gravity to vertical velocity
            Velocity.Y += Gravity * deltaTime;

            // Update position based on velocity
            Vector2 newPosition = Position + Velocity * deltaTime;

            // Check for collisions
            CollisionInfos collisionInfos = CheckCollisions(newPosition, collisionMapData, CollisionWidth, CollisionHeight);

            if (collisionInfos.Death)
            {
                Dead = true;
                DyingFX.Play();
                return;
            }
            else if (collisionInfos.Exit)
            {
                Won = true;
                MediaPlayer.Stop();
                WinningFX.Play();
                return;
            }

            // Update position and velocity based on collision info
            if (collisionInfos.Left)
            {
                Velocity.X = 200f;
            }
            else if (collisionInfos.Right)
            {
                Velocity.X = -200f;
            }
            else
            {
                Position.X = newPosition.X;
            }

            if (collisionInfos.Top || collisionInfos.Bottom)
            {
                Velocity.Y = 0; // Stop vertical movement if colliding vertically
                if (collisionInfos.Bottom)
                {
                    isOnGround = true; // Player is on the ground
                    if (_currentAnimationState == AnimationState.Falling)
                    {
                        _currentAnimationState = AnimationState.Landing;
                        _currentJumpLandFrame = 0;
                        _jumpAnimationTime = 0f;
                    }
                    else if (!isMoving && _currentAnimationState != AnimationState.Landing)
                    {
                        _currentAnimationState = AnimationState.Idle;
                    }
                }
                else
                {
                    Velocity.Y = 200f;
                }
            }
            else
            {
                Position.Y = newPosition.Y;
                isOnGround = false; // Player is in the air
                if (_currentAnimationState == AnimationState.Jumping && _currentJumpStartFrame >= JumpStartFrames.Count)
                {
                    _currentAnimationState = AnimationState.Falling;
                }
            }

            // Update animation frame if moving
            if (isMoving && isOnGround)
            {
                _timeForCurrentFrame += deltaTime;
                if (_timeForCurrentFrame >= _frameTime)
                {
                    _currentFrame = (_currentFrame + 1) % WalkFrames.Count;
                    _timeForCurrentFrame = 0f;
                }
                _currentAnimationState = AnimationState.Walking;
            }

            // Handle shooting
            if (state.IsKeyDown(Keys.Space) && _laserCooldownRemaining <= 0f)
            {
                ShootLaser();
                _laserCooldownRemaining = LaserCooldown;
            }

            // Update lasers
            foreach (var laser in _lasers)
            {
                laser.Update(gameTime, collisionMapData, enemies);
            }
            _lasers.RemoveAll(l => !l.Active);

            // Check for collision with enemies
            foreach (var enemy in enemies)
            {
                if (!enemy.Dead && new Rectangle((int)Position.X + (int)CollisionOffsetX, (int)Position.Y + (int)CollisionOffsetY, (int)(CollisionWidth), (int)(CollisionHeight)).Intersects(new Rectangle((int)enemy.Position.X, (int)enemy.Position.Y, (int)(Enemy.Texture.Width * enemy.Scale), (int)(Enemy.Texture.Height * enemy.Scale))))
                {
                    Dead = true;
                    DyingFX.Play();
                    return;
                }
            }
            // Update jump animations
            if (_currentAnimationState == AnimationState.Jumping)
            {
                _jumpAnimationTime += deltaTime;
                if (_jumpAnimationTime >= _frameTime)
                {
                    _currentJumpStartFrame++;
                    _jumpAnimationTime = 0f;
                    if (_currentJumpStartFrame >= JumpStartFrames.Count)
                    {
                        _currentAnimationState = AnimationState.Falling;
                    }
                }
            }
            else if (_currentAnimationState == AnimationState.Landing)
            {
                _jumpAnimationTime += deltaTime;
                if (_jumpAnimationTime >= _frameTime)
                {
                    _currentJumpLandFrame++;
                    _jumpAnimationTime = 0f;
                    if (_currentJumpLandFrame >= JumpLandFrames.Count)
                    {
                        _currentAnimationState = isMoving ? AnimationState.Walking : AnimationState.Idle;
                    }
                }
            }
        }

        private void ShootLaser()
        {
            float laserOffsetY = CollisionHeight * 0.25f;
            Vector2 laserPosition;
            if (facingRight)
            {
                laserPosition = new Vector2(Position.X + CollisionWidth + CollisionOffsetX, Position.Y + laserOffsetY + CollisionOffsetY);
            }
            else
            {
                laserPosition = new Vector2(Position.X + CollisionOffsetX, Position.Y + laserOffsetY + CollisionOffsetY);
            }
            Vector2 laserVelocity = facingRight ? new Vector2(LaserSpeed, 0) : new Vector2(-LaserSpeed, 0);
            Laser laser = new Laser(laserPosition, laserVelocity);
            _lasers.Add(laser);
            Laser.ShootingFX.Play();
        }

        private CollisionInfos CheckCollisions(Vector2 position, Color[,] collisionMapData, float width, float height)
        {
            CollisionInfos collisionInfos = new CollisionInfos();

            int left = (int)position.X + (int)CollisionOffsetX;
            int right = (int)(position.X + (int)CollisionOffsetX + width);
            int top = (int)position.Y + (int)CollisionOffsetY;
            int bottom = (int)(position.Y + (int)CollisionOffsetY + height);

            // Check collision for left and right edges
            if (Velocity.X < 0) // Moving left
            {
                var leftTopCollision = CollisionCheck(left, top, collisionMapData);
                var leftBottomCollision = CollisionCheck(left, bottom - 5, collisionMapData);
                if (leftTopCollision == CollisionType.Death || leftBottomCollision == CollisionType.Death)
                {
                    collisionInfos.Death = true;
                }
                else if (leftTopCollision == CollisionType.Exit || leftBottomCollision == CollisionType.Exit)
                {
                    collisionInfos.Exit = true;
                }
                else if (leftTopCollision == CollisionType.Solid || leftBottomCollision == CollisionType.Solid)
                {
                    collisionInfos.Left = true;
                }
            }
            else if (Velocity.X > 0) // Moving right
            {
                var rightTopCollision = CollisionCheck(right, top, collisionMapData);
                var rightBottomCollision = CollisionCheck(right, bottom - 5, collisionMapData);
                if (rightTopCollision == CollisionType.Death || rightBottomCollision == CollisionType.Death)
                {
                    collisionInfos.Death = true;
                }
                else if (rightTopCollision == CollisionType.Exit || rightBottomCollision == CollisionType.Exit)
                {
                    collisionInfos.Exit = true;
                }
                else if (rightTopCollision == CollisionType.Solid || rightBottomCollision == CollisionType.Solid)
                {
                    collisionInfos.Right = true;
                }
            }

            // Check collision for top and bottom edges
            if (Velocity.Y < 0) // Moving up
            {
                var topLeftCollision = CollisionCheck(left, top, collisionMapData);
                var topRightCollision = CollisionCheck(right - 5, top, collisionMapData);
                if (topLeftCollision == CollisionType.Death || topRightCollision == CollisionType.Death)
                {
                    collisionInfos.Death = true;
                }
                else if (topLeftCollision == CollisionType.Exit || topRightCollision == CollisionType.Exit)
                {
                    collisionInfos.Exit = true;
                }
                else if (topLeftCollision == CollisionType.Solid || topRightCollision == CollisionType.Solid)
                {
                    collisionInfos.Top = true;
                }
            }
            else if (Velocity.Y > 0) // Moving down
            {
                var bottomLeftCollision = CollisionCheck(left, bottom, collisionMapData);
                var bottomRightCollision = CollisionCheck(right - 5, bottom, collisionMapData);
                if (bottomLeftCollision == CollisionType.Death || bottomRightCollision == CollisionType.Death)
                {
                    collisionInfos.Death = true;
                }
                else if (bottomLeftCollision == CollisionType.Exit || bottomRightCollision == CollisionType.Exit)
                {
                    collisionInfos.Exit = true;
                }
                else if (bottomLeftCollision == CollisionType.Solid || bottomRightCollision == CollisionType.Solid)
                {
                    collisionInfos.Bottom = true;
                }
            }

            return collisionInfos;
        }

        private CollisionType CollisionCheck(int x, int y, Color[,] collisionMapData)
        {
            // Check if the position is within the bounds of the collision map
            if (x < 0 || x >= collisionMapData.GetLength(0) || y < 0 || y >= collisionMapData.GetLength(1))
                return CollisionType.Solid;

            // Get the color of the pixel at the position
            Color pixelColor = collisionMapData[x, y];

            // Switch based on the color of the pixel
            switch (pixelColor)
            {
                case var color when color == Color.Black:
                    return CollisionType.Solid;
                case var color when color == Color.Red:
                    return CollisionType.Death;
                case var color when color == new Color(0, 255, 0): // green
                    return CollisionType.Exit;
                default:
                    return CollisionType.None;
            }
        }

        // useful to debug collision box
        public void DrawCollisionRectangle(SpriteBatch spriteBatch, Texture2D texture)
        {
            Rectangle collisionRectangle = new Rectangle(
                    (int)(Position.X + CollisionOffsetX),
                    (int)(Position.Y + CollisionOffsetY),
                    (int)CollisionWidth,
                    (int)CollisionHeight);
            spriteBatch.Draw(texture, collisionRectangle, Color.Red * 0.5f); // Draw with some transparency
        }

        public void HandleDeath(GameTime gameTime)
        {
            if (!deathHandled)
            {
                Velocity = new Vector2(300, -1200); // Throw the player in the top right direction
                deathHandled = true;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Velocity.Y += Gravity * deltaTime; // Apply gravity
            Position += Velocity * deltaTime; // Update position

            Scale += deltaTime * 0.15f; // Adjust the value 0.5f to control the rate of scaling
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            SpriteEffects spriteEffect = facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (Dead)
            {
                spriteEffect |= SpriteEffects.FlipVertically; // Flip the sprite vertically if dead
            }
            Texture2D currentFrameTexture = null;

            switch (_currentAnimationState)
            {
                case AnimationState.Idle:
                    currentFrameTexture = IdleFrame;
                    break;
                case AnimationState.Walking:
                    currentFrameTexture = WalkFrames[_currentFrame];
                    break;
                case AnimationState.Jumping:
                    currentFrameTexture = JumpStartFrames[Math.Min(_currentJumpStartFrame, JumpStartFrames.Count - 1)];
                    break;
                case AnimationState.Falling:
                    currentFrameTexture = JumpFlyFrame;
                    break;
                case AnimationState.Landing:
                    currentFrameTexture = JumpLandFrames[Math.Min(_currentJumpLandFrame, JumpLandFrames.Count - 1)];
                    break;
            }

            spriteBatch.Draw(currentFrameTexture, Position, null, Color.White, 0f, Vector2.Zero, Scale, spriteEffect, 0f);

            foreach (var laser in _lasers)
            {
                laser.Draw(spriteBatch);
            }
        }

        private class CollisionInfos
        {
            public bool Left { get; set; }
            public bool Right { get; set; }
            public bool Top { get; set; }
            public bool Bottom { get; set; }
            public bool Death { get; set; }
            public bool Exit { get; set; }

        }

        enum CollisionType
        {
            None,
            Solid,
            Death,
            Exit,
        }
    }

    public class Laser
    {
        public static Texture2D Texture;
        public static SoundEffectInstance ShootingFX;

        public Vector2 Position;
        public Vector2 Velocity;
        public bool Active;

        public Laser(Vector2 position, Vector2 velocity)
        {
            Position = position;
            Velocity = velocity;
            Active = true;
        }

        public void Update(GameTime gameTime, Color[,] collisionMapData, List<Enemy> enemies)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Velocity * deltaTime;

            // Check for collision with environment
            if (Position.X < 0 || Position.X >= collisionMapData.GetLength(0) || Position.Y < 0 || Position.Y >= collisionMapData.GetLength(1) || collisionMapData[(int)Position.X, (int)Position.Y].A != 0)
            {
                Active = false;
            }

            // Check for collision with enemies
            foreach (var enemy in enemies)
            {
                if (Active && !enemy.Dead && new Rectangle((int)enemy.Position.X, (int)enemy.Position.Y, (int)(Enemy.Texture.Width * enemy.Scale), (int)(Enemy.Texture.Height * enemy.Scale)).Contains(Position.ToPoint()))
                {
                    enemy.Dead = true;
                    Active = false;
                    Enemy.DyingFX.Play();
                    break;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (Active)
            {
                spriteBatch.Draw(Texture, Position, Color.White);
            }
        }
    }
}
