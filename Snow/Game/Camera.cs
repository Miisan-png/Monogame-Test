using Microsoft.Xna.Framework;

namespace Snow.Engine
{
    public class Camera
    {
        public Vector2 Position { get; set; }
        public int RoomWidth { get; set; }
        public int RoomHeight { get; set; }
        public int ViewWidth { get; private set; }
        public int ViewHeight { get; private set; }

        private Vector2 _currentRoomOrigin;

        public Camera(int viewWidth, int viewHeight, int roomWidth, int roomHeight)
        {
            ViewWidth = viewWidth;
            ViewHeight = viewHeight;
            RoomWidth = roomWidth;
            RoomHeight = roomHeight;
            Position = Vector2.Zero;
            _currentRoomOrigin = Vector2.Zero;
        }

        public void Follow(Vector2 targetPosition)
        {
            int roomX = (int)(targetPosition.X / RoomWidth) * RoomWidth;
            int roomY = (int)(targetPosition.Y / RoomHeight) * RoomHeight;

            _currentRoomOrigin = new Vector2(roomX, roomY);
            Position = _currentRoomOrigin;
        }

        public Matrix GetTransformMatrix()
        {
            return Matrix.CreateTranslation(-Position.X, -Position.Y, 0);
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return screenPosition + Position;
        }

        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return worldPosition - Position;
        }

        public bool IsInView(Vector2 position, float margin = 32f)
        {
            return position.X >= Position.X - margin &&
                   position.X <= Position.X + ViewWidth + margin &&
                   position.Y >= Position.Y - margin &&
                   position.Y <= Position.Y + ViewHeight + margin;
        }
    }
}






