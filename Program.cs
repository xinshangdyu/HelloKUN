using System.Diagnostics;

const int width = 24; // 游戏区域宽度（格子数）
const int height = 24; // 游戏区域高度（格子数）
const int tickMilliseconds = 120; // 每一帧更新间隔（毫秒）

Console.OutputEncoding = System.Text.Encoding.UTF8; // 启用 UTF-8 以正确显示中文和符号
Console.CursorVisible = false; // 隐藏光标，避免闪烁影响体验

var random = new Random(); // 随机数生成器（用于食物生成）
var score = 0; // 当前分数
var isRunning = true; // 游戏主循环开关
var isPaused = false; // 暂停状态

var snake = new LinkedList<Point>(new[] // 蛇身（头在 First）
{
    new Point(width / 2, height / 2), // 初始蛇头
    new Point(width / 2 - 1, height / 2), // 初始身体第 2 节
    new Point(width / 2 - 2, height / 2) // 初始身体第 3 节
});

var direction = Direction.Right; // 当前方向
var nextDirection = direction; // 下一帧方向（用于输入缓冲）
var food = SpawnFood(random, snake); // 初始食物位置

Render(); // 首帧渲染
var timer = Stopwatch.StartNew(); // 启动计时器控制帧率

while (isRunning) // 主循环
{
    HandleInput(); // 先处理输入

    if (isPaused) // 暂停时仅轮询输入
    {
        Thread.Sleep(20); // 降低 CPU 占用
        continue;
    }

    if (timer.ElapsedMilliseconds < tickMilliseconds) // 未到下一帧则短暂休眠
    {
        Thread.Sleep(1); // 避免空转
        continue;
    }

    timer.Restart(); // 开始下一帧计时
    Tick(); // 更新游戏状态
    Render(); // 绘制画面
}

Console.SetCursorPosition(0, height + 6); // 退出前把光标移到底部
Console.CursorVisible = true; // 恢复光标显示

void HandleInput()
{
    while (Console.KeyAvailable) // 清空输入缓冲区，避免按键积压
    {
        var key = Console.ReadKey(intercept: true).Key; // 读取按键且不回显
        switch (key)
        {
            case ConsoleKey.UpArrow or ConsoleKey.W:
                TrySetDirection(Direction.Up); // 向上
                break;
            case ConsoleKey.DownArrow or ConsoleKey.S:
                TrySetDirection(Direction.Down); // 向下
                break;
            case ConsoleKey.LeftArrow or ConsoleKey.A:
                TrySetDirection(Direction.Left); // 向左
                break;
            case ConsoleKey.RightArrow or ConsoleKey.D:
                TrySetDirection(Direction.Right); // 向右
                break;
            case ConsoleKey.Spacebar:
                isPaused = !isPaused; // 切换暂停
                break;
            case ConsoleKey.R:
                Restart(); // 重开
                break;
            case ConsoleKey.Escape or ConsoleKey.Q:
                isRunning = false; // 退出
                break;
        }
    }
}

void Tick()
{
    direction = nextDirection; // 提交输入缓冲方向
    var head = snake.First!.Value; // 获取当前蛇头
    var delta = direction switch // 方向转位移
    {
        Direction.Up => new Point(0, -1),
        Direction.Down => new Point(0, 1),
        Direction.Left => new Point(-1, 0),
        _ => new Point(1, 0)
    };

    var nextHead = new Point(head.X + delta.X, head.Y + delta.Y); // 计算下一位置

    if (nextHead.X < 0 || nextHead.X >= width || nextHead.Y < 0 || nextHead.Y >= height) // 撞墙检测
    {
        GameOver(); // 结束游戏
        return;
    }

    if (snake.Contains(nextHead)) // 撞到自身检测
    {
        GameOver(); // 结束游戏
        return;
    }

    snake.AddFirst(nextHead); // 蛇头前进

    if (nextHead == food) // 吃到食物
    {
        score += 10; // 加分
        food = SpawnFood(random, snake); // 重新生成食物
    }
    else
    {
        snake.RemoveLast(); // 未吃到食物则移除尾巴，保持长度
    }
}

void TrySetDirection(Direction candidate)
{
    if ((direction, candidate) is // 禁止 180° 反向掉头
        (Direction.Up, Direction.Down) or
        (Direction.Down, Direction.Up) or
        (Direction.Left, Direction.Right) or
        (Direction.Right, Direction.Left))
    {
        return; // 非法方向变更直接忽略
    }

    nextDirection = candidate; // 记录合法输入，下一帧生效
}

Point SpawnFood(Random rnd, LinkedList<Point> body)
{
    Point p; // 候选食物位置
    do
    {
        p = new Point(rnd.Next(0, width), rnd.Next(0, height)); // 随机落点
    } while (body.Contains(p)); // 避免与蛇身重叠

    return p; // 返回有效食物点
}

void Restart()
{
    snake.Clear(); // 清空旧蛇身
    snake.AddLast(new Point(width / 2, height / 2)); // 重置蛇头
    snake.AddLast(new Point(width / 2 - 1, height / 2)); // 重置第 2 节
    snake.AddLast(new Point(width / 2 - 2, height / 2)); // 重置第 3 节
    direction = Direction.Right; // 重置方向
    nextDirection = direction; // 同步输入缓冲
    score = 0; // 分数清零
    isPaused = false; // 取消暂停
    food = SpawnFood(random, snake); // 重新生成食物
    Render(); // 立即刷新显示
}

void GameOver()
{
    isPaused = true; // 进入暂停态（等待玩家操作）
    Console.SetCursorPosition(0, height + 4); // 把提示写在棋盘下方
    Console.Write("游戏结束！按 R 重开，按 Q 或 Esc 退出。              "); // 输出结束提示
}

void Render()
{
    Console.SetCursorPosition(0, 0); // 从左上角重绘，形成“动画”效果

    Console.WriteLine($"C# 贪吃蛇  分数: {score}                "); // 标题与分数
    Console.WriteLine("控制: 方向键/WASD 移动, 空格暂停, R重开, Q退出"); // 控制说明

    for (var y = -1; y <= height; y++) // 包含外边框行
    {
        for (var x = -1; x <= width; x++) // 包含外边框列
        {
            if (y == -1 || y == height || x == -1 || x == width) // 边框位置
            {
                Console.Write('■'); // 绘制边框
                continue;
            }

            var p = new Point(x, y); // 当前网格坐标
            if (p == snake.First!.Value)
            {
                Console.Write('●'); // 蛇头
            }
            else if (snake.Contains(p))
            {
                Console.Write('○'); // 蛇身
            }
            else if (p == food)
            {
                Console.Write('★'); // 食物
            }
            else
            {
                Console.Write(' '); // 空白地面
            }
        }

        Console.WriteLine(); // 换行进入下一行网格
    }

    Console.WriteLine(isPaused ? "[已暂停] 按空格继续。                         " : "                                               "); // 底部状态行
}

enum Direction
{
    Up,
    Down,
    Left,
    Right
}

readonly record struct Point(int X, int Y); // 不可变点坐标
