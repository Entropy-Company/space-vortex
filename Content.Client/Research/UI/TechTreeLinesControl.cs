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
    /// Поддерживает цветовую дифференциацию и стрелочки для указания направления.
    /// </summary>
    public sealed class TechTreeLinesControl : Control
    {
        /// <summary>
        /// Центры узлов (технологий) на экране
        /// </summary>
        public Dictionary<string, System.Numerics.Vector2> NodeCenters = new();

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

            // Группируем все входящие и исходящие связи для каждой вершины
            var incoming = new Dictionary<string, List<string>>();
            var outgoing = new Dictionary<string, List<string>>();
            foreach (var (from, to) in Edges)
            {
                if (!outgoing.ContainsKey(from)) outgoing[from] = new();
                outgoing[from].Add(to);
                if (!incoming.ContainsKey(to)) incoming[to] = new();
                incoming[to].Add(from);
            }

            // Отрисовываем каждую связь между технологиями
            foreach (var (from, to) in Edges)
            {
                if (!NodeCenters.TryGetValue(from, out var fromPos) || !NodeCenters.TryGetValue(to, out var toPos))
                    continue;

                // Определяем цвет линии на основе статуса целевой технологии
                var lineColor = GetLineColor(to);

                // Создаем угловые линии для лучшей читаемости
                const float halfTile = 40f;
                Vector2 p1, p2, p3, p4;

                if (MathF.Abs(toPos.X - fromPos.X) > 1f) // не строго под prereq
                {
                    // Определяем, из какой стороны выходить
                    if (toPos.X < fromPos.X)
                    {
                        // Левее — из левой грани
                        p1 = new Vector2(fromPos.X - halfTile, fromPos.Y);
                    }
                    else
                    {
                        // Правее — из правой грани
                        p1 = new Vector2(fromPos.X + halfTile, fromPos.Y);
                    }
                    p2 = new Vector2(toPos.X, fromPos.Y); // горизонтально к X ребёнка
                    p3 = new Vector2(toPos.X, toPos.Y - halfTile); // вниз к уровню ребёнка
                    p4 = new Vector2(toPos.X, toPos.Y); // центр ребёнка
                }
                else
                {
                    // Строго под prereq — прямая линия
                    p1 = new Vector2(fromPos.X, fromPos.Y + halfTile);
                    p2 = new Vector2(fromPos.X, toPos.Y - halfTile);
                    p3 = new Vector2(toPos.X, toPos.Y - halfTile);
                    p4 = new Vector2(toPos.X, toPos.Y);
                }

                // Проверяем, попадает ли хотя бы одна точка в видимую область
                var view = new Box2(Vector2.Zero, Size);
                if (!view.Contains(p1) && !view.Contains(p2) && !view.Contains(p3) && !view.Contains(p4))
                    continue;

                // Рисуем линии связи
                DrawThickLine(handle, p1, p2, lineColor);
                DrawThickLine(handle, p2, p3, lineColor);
                DrawThickLine(handle, p3, p4, lineColor);

                // Рисуем стрелочку на последнем сегменте (p3 -> p4) ближе к целевой технологии
                DrawArrow(handle, p3, p4, lineColor);
            }
        }

        /// <summary>
        /// Определяет цвет линии на основе статуса технологии
        /// </summary>
        private Color GetLineColor(string technologyId)
        {
            if (!NodeStatuses.TryGetValue(technologyId, out var status))
                return Color.Gray; // По умолчанию серый для неизвестных технологий

            return status switch
            {
                ResearchAvailablity.Researched => Color.LimeGreen, // Зеленый для исследованных
                ResearchAvailablity.Available => Color.Yellow, // Желтый для доступных
                ResearchAvailablity.Unavailable => Color.Red, // Красный для недоступных
                _ => Color.Gray
            };
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
        /// Рисует стрелочку на конце линии для указания направления
        /// </summary>
        private void DrawArrow(DrawingHandleScreen handle, Vector2 from, Vector2 to, Color color)
        {
            const float arrowSize = 10f; // Увеличиваем размер стрелки
            const float arrowAngle = 0.4f; // угол стрелки в радианах
            const float arrowOffset = 15f; // отступ от конца линии

            var direction = Vector2.Normalize(to - from);
            var arrowTip = to - direction * arrowOffset; // отступаем от конца

            // Вычисляем точки стрелки
            var leftArrow = arrowTip + new Vector2(
                direction.X * MathF.Cos(arrowAngle) - direction.Y * MathF.Sin(arrowAngle),
                direction.X * MathF.Sin(arrowAngle) + direction.Y * MathF.Cos(arrowAngle)
            ) * arrowSize;

            var rightArrow = arrowTip + new Vector2(
                direction.X * MathF.Cos(-arrowAngle) - direction.Y * MathF.Sin(-arrowAngle),
                direction.X * MathF.Sin(-arrowAngle) + direction.Y * MathF.Cos(-arrowAngle)
            ) * arrowSize;

            // Рисуем тень стрелочки (черная с небольшим смещением)
            var shadowOffset = new Vector2(1f, 1f);
            handle.DrawLine(arrowTip + shadowOffset, leftArrow + shadowOffset, Color.Black);
            handle.DrawLine(arrowTip + shadowOffset, rightArrow + shadowOffset, Color.Black);

            // Рисуем стрелочку
            handle.DrawLine(arrowTip, leftArrow, color);
            handle.DrawLine(arrowTip, rightArrow, color);

            // Добавляем небольшую линию от стрелки к концу для лучшей видимости
            handle.DrawLine(arrowTip, to, color);

            // Рисуем дополнительную линию для усиления стрелки
            handle.DrawLine(leftArrow, rightArrow, color);
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
