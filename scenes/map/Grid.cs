using Godot;
using System;

public partial class Grid : ColorRect
{
    [Signal]
    public delegate void GridClickedEventHandler(Vector2 worldPosition);

    private Vector2 _shipPosition;
    private Camera2D _camera;
    private Font _font;
    private Vector2? _lastClickWorldPos = null;
    private Rect2 _gridArea;
    private float _currentMajorLineWidth = 1.0f;
    private float _currentMinorLineWidth = 1.0f;

    [ExportCategory("Настройки сетки")]
    [Export] public float GridSizeWorld { get; private set; } = 100.0f;
    [Export] public float LineWidth { get; private set; } = 2.0f;
    [Export] public Color MajorLineColor { get; private set; } = new Color(1.0f, 1.0f, 1.0f, 0.6f);
    [Export] public Color MinorLineColor { get; private set; } = new Color(1.0f, 1.0f, 1.0f, 0.3f);
    [Export] public int MajorLineInterval { get; private set; } = 5;

    [ExportCategory("Настройки отступов и границ")]
    [Export] public float Padding { get; private set; } = 50.0f;
    [Export] public Color BorderColor { get; private set; } = new Color(0, 0, 0, 0.8f);
    [Export] public float BorderWidth { get; private set; } = 3.0f;

    [ExportCategory("Настройки текста")]
    [Export] public int FontSize { get; private set; } = 16;
    [Export] public Color TextColor { get; private set; } = new Color(0, 0, 0, 0.8f);
    [Export] public float MinTextSpacing { get; private set; } = 80.0f;

    [ExportCategory("Настройки маркера")]
    [Export] public Color MarkerColor { get; private set; } = new Color(1, 0, 0, 0.8f);
    [Export] public float MarkerSize { get; private set; } = 10.0f;

    public override void _Ready()
    {
        _camera = GetViewport().GetCamera2D();
        _font = GetThemeDefaultFont();

        GuiInput += OnGuiInput;
    }

    public void UpdateGrid(Vector2 centerPosition)
    {
        _shipPosition = centerPosition;

        _currentMajorLineWidth = LineWidth;
        //_currentMajorLineWidth = LineWidth * _camera.Zoom.X;
        _currentMinorLineWidth = _currentMajorLineWidth / 2;

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_camera == null) return;

        var viewportSize = GetViewportRect().Size;
        _gridArea = new Rect2(Padding, Padding, viewportSize.X - Padding * 2, viewportSize.Y - Padding * 2);

        DrawGrid();
        DrawMarker();
        DrawBorder();
    }

    private void DrawGrid()
    {
        var zoom = _camera.Zoom.X;

        float leftWorld = _shipPosition.X - (_gridArea.Size.X / 2 / zoom);
        float rightWorld = _shipPosition.X + (_gridArea.Size.X / 2 / zoom);
        float topWorld = _shipPosition.Y - (_gridArea.Size.Y / 2 / zoom);
        float bottomWorld = _shipPosition.Y + (_gridArea.Size.Y / 2 / zoom);

        float majorGridStep = GridSizeWorld * MajorLineInterval;

        // Вертикальные линии.
        float worldX = Mathf.Floor(leftWorld / GridSizeWorld) * GridSizeWorld;
        for (; worldX <= rightWorld; worldX += GridSizeWorld)
        {
            float screenX = _gridArea.Position.X + _gridArea.Size.X / 2 + (worldX - _shipPosition.X) * zoom;

            if (screenX >= _gridArea.Position.X && screenX <= _gridArea.Position.X + _gridArea.Size.X)
            {
                var from = new Vector2(screenX, _gridArea.Position.Y);
                var to = new Vector2(screenX, _gridArea.Position.Y + _gridArea.Size.Y);

                bool isMajor = Mathf.Abs(Mathf.PosMod(worldX, majorGridStep)) < 0.01f;
                var color = isMajor ? MajorLineColor : MinorLineColor;
                float width = isMajor ? _currentMajorLineWidth : _currentMinorLineWidth;

                DrawLine(from, to, color, width);

                // Если приближение камеры слишком маленькое, скрываем минорный текст.
                if (_camera.Zoom.X <= 0.3 && !isMajor) 
                {
                    continue;
                }

                // Текст координат.
                string xText = worldX.ToString();
                var textSize = _font.GetStringSize(xText, HorizontalAlignment.Center, -1, FontSize);

                // Верхний текст.
                float topTextY = _gridArea.Position.Y - textSize.Y;
                var topTextPos = new Vector2(screenX - textSize.X / 2, topTextY);
                DrawString(_font, topTextPos, xText, HorizontalAlignment.Center, -1, FontSize, TextColor);

                // Нижний текст.
                float bottomTextY = _gridArea.Position.Y + _gridArea.Size.Y + textSize.Y;
                var bottomTextPos = new Vector2(screenX - textSize.X / 2, bottomTextY);
                DrawString(_font, bottomTextPos, xText, HorizontalAlignment.Center, -1, FontSize, TextColor);
            }
        }

        // Горизонтальные линии.
        float worldY = Mathf.Floor(topWorld / GridSizeWorld) * GridSizeWorld;
        for (; worldY <= bottomWorld; worldY += GridSizeWorld)
        {
            float screenY = _gridArea.Position.Y + _gridArea.Size.Y / 2 + (worldY - _shipPosition.Y) * zoom;

            // Рисуем только внутри области сетки
            if (screenY >= _gridArea.Position.Y && screenY <= _gridArea.Position.Y + _gridArea.Size.Y)
            {
                var from = new Vector2(_gridArea.Position.X, screenY);
                var to = new Vector2(_gridArea.Position.X + _gridArea.Size.X, screenY);

                bool isMajor = Mathf.Abs(Mathf.PosMod(worldY, majorGridStep)) < 0.01f;
                var color = isMajor ? MajorLineColor : MinorLineColor;
                float width = isMajor ? _currentMajorLineWidth : _currentMinorLineWidth;

                DrawLine(from, to, color, width);

                // Если приближение камеры слишком маленькое, скрываем минорный текст.
                if (_camera.Zoom.X <= 0.3 && !isMajor)
                {
                    continue;
                }

                // Текст координат.
                string yText = worldY.ToString();
                var textSize = _font.GetStringSize(yText, HorizontalAlignment.Center, -1, FontSize);

                // Левый текст.
                float leftTextX = _gridArea.Position.X - textSize.X - 5;
                var leftTextPos = new Vector2(leftTextX, screenY + textSize.Y / 2);
                DrawString(_font, leftTextPos, yText, HorizontalAlignment.Right, -1, FontSize, TextColor);

                // Правый текст.
                float rightTextX = _gridArea.Position.X + _gridArea.Size.X + 5;
                var rightTextPos = new Vector2(rightTextX, screenY + textSize.Y / 2);
                DrawString(_font, rightTextPos, yText, HorizontalAlignment.Left, -1, FontSize, TextColor);
            }
        }
    }

    private void DrawMarker()
    {
        if (_lastClickWorldPos.HasValue)
        {
            var worldPos = _lastClickWorldPos.Value;
            var screenPos = WorldToScreenPosition(worldPos);

            if (_gridArea.HasPoint(screenPos))
            {
                DrawCircle(screenPos, MarkerSize, MarkerColor);
            }
        }
    }

    private void DrawBorder()
    {
        DrawRect(_gridArea, BorderColor, false, BorderWidth, true);
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                var clickScreenPos = mouseEvent.Position;

                if (_gridArea.HasPoint(clickScreenPos))
                {
                    var worldPos = ScreenToWorldPosition(clickScreenPos);
                    var cellCenter = GetCellCenter(worldPos);

                    _lastClickWorldPos = cellCenter;
                    EmitSignal(SignalName.GridClicked, cellCenter);
                }
            }
        }
    }

    /// <summary>
    /// Преобразовать экранные координаты в мировые.
    /// </summary>
    public Vector2 ScreenToWorldPosition(Vector2 screenPos)
    {
        if (_camera == null)
            return Vector2.Zero;

        var zoom = _camera.Zoom.X;

        float relativeX = screenPos.X - (_gridArea.Position.X + _gridArea.Size.X / 2);
        float relativeY = screenPos.Y - (_gridArea.Position.Y + _gridArea.Size.Y / 2);

        return _shipPosition + new Vector2(relativeX, relativeY) / zoom;
    }

    /// <summary>
    /// Преобразовать мировые координаты в экранные.
    /// </summary>
    public Vector2 WorldToScreenPosition(Vector2 worldPos)
    {
        if (_camera == null)
        {
            return Vector2.Zero;
        }

        var zoom = _camera.Zoom.X;

        Vector2 relative = (worldPos - _shipPosition) * zoom;

        return new Vector2(
            _gridArea.Position.X + _gridArea.Size.X / 2 + relative.X,
            _gridArea.Position.Y + _gridArea.Size.Y / 2 + relative.Y
        );
    }

    /// <summary>
    /// Получить центр ячейки сетки для указанных мировых координат.
    /// </summary>
    public Vector2 GetCellCenter(Vector2 worldPos)
    {
        int cellX = (int)Mathf.Floor(worldPos.X / GridSizeWorld);
        int cellY = (int)Mathf.Floor(worldPos.Y / GridSizeWorld);

        return new Vector2((cellX + 0.5f) * GridSizeWorld, (cellY + 0.5f) * GridSizeWorld);
    }

    public void SetMarker(Vector2 worldPosition, bool snapToCellCenter = true)
    {
        if (snapToCellCenter)
        {
            _lastClickWorldPos = GetCellCenter(worldPosition);
        }
        else
        {
            _lastClickWorldPos = worldPosition;
        }

        QueueRedraw();
    }

    public void ClearMarker()
    {
        _lastClickWorldPos = null;

        QueueRedraw();
    }
}