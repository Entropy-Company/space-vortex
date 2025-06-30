using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client.Research.UI
{
    /// <summary>
    /// Контрол для отрисовки линий связей между технологиями в дереве исследований.
    /// </summary>
    public sealed class TechTreeLinesControl : Control
    {
        /// <summary>
        /// Центры узлов (технологий) на экране
        /// </summary>
        public Dictionary<string, Vector2> NodeCenters = new();

        /// <summary>
        /// Список связей между технологиями (From -> To)
        /// </summary>
        public List<(string From, string To)> Edges = new();

        /// <summary>
        /// Статус каждой технологии для определения цвета линий
        /// </summary>
        public Dictionary<string, ResearchAvailablity> NodeStatuses = new();

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);
            foreach (var (from, to) in Edges)
            {
                if (!NodeCenters.TryGetValue(from, out var fromPos) || !NodeCenters.TryGetValue(to, out var toPos))
                    continue;

                // Ортогональный маршрут: сначала по X, потом по Y
                if (MathF.Abs(fromPos.X - toPos.X) > 1f)
                {
                    var mid = new Vector2(toPos.X, fromPos.Y);
                    handle.DrawLine(fromPos, mid, Color.White);
                    handle.DrawLine(mid, toPos, Color.White);
                }
                else
                {
                    handle.DrawLine(fromPos, toPos, Color.White);
                }
            }
        }

        /// <summary>
        /// Рисует "толстую" линию с помощью множественных линий
        /// </summary>
        private void DrawThickLine(DrawingHandleScreen handle, Vector2 from, Vector2 to, Color color)
        {
            // Основная линия
            handle.DrawLine(from, to, color);

            // Дополнительные линии для создания эффекта толщины
            var direction = Vector2.Normalize(to - from);
            var perpendicular = new Vector2(-direction.Y, direction.X);

            // Смещения для создания толщины
            var offsets = new[] { 0.5f, -0.5f };

            foreach (var offset in offsets)
            {
                var offsetVector = perpendicular * offset;
                handle.DrawLine(from + offsetVector, to + offsetVector, color);
            }
        }

        /// <summary>
        /// Рисует стрелку на линии, указывающую направление (от prerequisite к unlockable)
        /// </summary>
        private void DrawArrow(DrawingHandleScreen handle, Vector2 from, Vector2 to, Color color)
        {
            // Стрелка рисуется на 80% пути к to
            var dir = Vector2.Normalize(to - from);
            var arrowPos = from + dir * ((to - from).Length() * 0.8f);
            const float arrowSize = 12f;
            const float angle = 0.5f; // ~30 градусов
            // Левая часть стрелки
            var left = new Vector2(
                arrowPos.X + arrowSize * (float)Math.Cos(Math.Atan2(dir.Y, dir.X) + Math.PI - angle),
                arrowPos.Y + arrowSize * (float)Math.Sin(Math.Atan2(dir.Y, dir.X) + Math.PI - angle));
            // Правая часть стрелки
            var right = new Vector2(
                arrowPos.X + arrowSize * (float)Math.Cos(Math.Atan2(dir.Y, dir.X) + Math.PI + angle),
                arrowPos.Y + arrowSize * (float)Math.Sin(Math.Atan2(dir.Y, dir.X) + Math.PI + angle));
            handle.DrawLine(arrowPos, left, color);
            handle.DrawLine(arrowPos, right, color);
        }

        /// <summary>
        /// Принудительно перерисовывает контрол
        /// </summary>
        public void QueueRedraw()
        {
            // Force a redraw
            InvalidateMeasure();
            InvalidateArrange();
        }
    }
}
