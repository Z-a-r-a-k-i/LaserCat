using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace LaserCat
{
    public class Enemy
    {
        public static SoundEffectInstance DyingFX;
        public static Texture2D Texture;

        public static Random rand = new Random();

        public float Scale = 0.05f;
        private float _speed = 135f;
        public Vector2 Position;
        private float _leftBoundary;
        private float _rightBoundary;
        private bool _movingRight;
        private bool facingRight;
        public bool Dead = false;
        private bool deathHandled = false;
        private Vector2 Velocity;

        public Enemy(float intialY, float leftBoundary, float rightBoundary)
        {
            Position.Y = intialY;
            _leftBoundary = leftBoundary;
            _rightBoundary = rightBoundary;
            _movingRight = true;

            // randomize initial position
            Position.X = leftBoundary + rand.Next() % (rightBoundary - leftBoundary);
        }

        public void Update(GameTime gameTime)
        {
            if (Dead)
            {
                HandleDeath(gameTime);
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_movingRight)
            {
                Position.X += _speed * deltaTime;
                if (Position.X >= _rightBoundary)
                {
                    Position.X = _rightBoundary;
                    _movingRight = false;
                }
                else
                {
                    facingRight = true;
                }
            }
            else
            {
                Position.X -= _speed * deltaTime;
                if (Position.X <= _leftBoundary)
                {
                    Position.X = _leftBoundary;
                    _movingRight = true;
                }
                else
                {
                    facingRight = false;
                }
            }
        }

        public void HandleDeath(GameTime gameTime)
        {
            if (!deathHandled)
            {
                Velocity = new Vector2(250, -250); // Throw the enemy in the top right direction
                deathHandled = true;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Velocity.Y += 1000f * deltaTime; // Apply gravity
            Position += Velocity * deltaTime; // Update position

        }

        public void Draw(SpriteBatch spriteBatch)
        {
            SpriteEffects spriteEffect = facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (Dead)
            {
                spriteEffect |= SpriteEffects.FlipVertically; // Flip the sprite vertically if dead
            }
            spriteBatch.Draw(Texture, Position, null, Color.White, 0f, Vector2.Zero, Scale, spriteEffect, 0f);
        }
    }
}
