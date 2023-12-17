using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
	private enum FreezeType
	{
		None	 = 0,
		Ready	 = (1 << 1),  // New round started	
        EatGhost = (1 << 2),  // Pacman has eaten a ghost
        Dead     = (1 << 3),  // Pacman was eaten by a ghost
		Won		 = (1 << 4),  // round won (all dots eaten)
		GameOver = (1 << 5),  // round lost and no lifes left
		Reset    = (1 << 6),  // for freeze the game when reset is called (to avoid update actors in the first tick)
    }

    public enum FruitType
    {
        Cherries,
        Strawberry,
        Peach,
        Apple,
        Grapes,
        Galaxian,
        Bell,
        Key
    }

    // game constants

	private readonly Vector2I fruitTile = new Vector2I(112, 140) / Maze.TileSize;

	private readonly int[] ghostEatenScores = new int[] { 200, 400, 800, 1600 };
	private readonly int[] fruitScores = new int[] { 100, 300, 500, 700, 1000, 2000, 3000, 5000 };
	private readonly int dotScore = 10;
	private readonly int pillScore = 50;

	private readonly int ghostFrightenedTicks = 6 * 60;
	private readonly int ghostEatenFreezeTicks = 60;
	private readonly int pacmanEatenFreezeTicks = 60;
	private readonly int pacmanDeathTicks = 150;
	private readonly int roundWonFreezeTicks = 4 * 60;
	private readonly int fruitActiveTicks = 560;

    // scenes pacman and ghosts

    [Export]
	private PackedScene pacmanScene;

	[Export]
	private PackedScene ghostScene;

    [Export]
    private Texture2D dotsTexture;

	[Export]
	private Texture2D readyTextTexture;

	[Export]
	private Texture2D gameOverTextTexture;

	[Export]
	private Texture2D lifeTexture;

	[Export]
	private Texture2D fruitTexture;

	private Label scoreText;
	private Label highScoreText;
	private Sprite2D mazeSprite;
	private ColorRect ghostDoorSprite; // "sprite"

    private Pacman pacman;
	private Ghost[] ghosts = new Ghost[4];

	// sounds

	private AudioStreamPlayer munch1Sound;
    private AudioStreamPlayer munch2Sound;
	private AudioStreamPlayer fruitSound;
	private AudioStreamPlayer ghostEatenSound;
	private AudioStreamPlayer sirenSound;
	private AudioStreamPlayer powerPelletSound;

    // game control variables

    private int ticks;
	private int freeze;
	private int level;
	private int score;
	private int highScore;
	private int numGhostsEaten;
	private int numLifes;
	private int numDotsEaten;
	private int numDotsEatenThisRound;

    // triggers

    private List<Trigger> triggers = new List<Trigger>();
    private Trigger dotEatenTrigger;
	private Trigger pillEatenTrigger;
    private Trigger ghostEatenUnFreezeTrigger;
    private Trigger pacmanEatenTrigger;
	private Trigger readyStartedTrigger;
	private Trigger roundStartedTrigger;
	private Trigger roundWonTrigger;
	private Trigger gameOverTrigger;
	private Trigger resetTrigger;
	private Trigger fruitActiveTrigger;
	private Trigger fruitEatenTrigger;
	private Trigger[] ghostFrightenedTrigger = new Trigger[4];
	private Trigger[] ghostEatenTrigger = new Trigger[4];

	// debug

	private List<Vector2I>[] ghostsPaths = new List<Vector2I>[4];

	/* LOAD AND SAVE HIGHSCORE */

	private void LoadHighScore()
	{
		FileAccess highScoreFile = FileAccess.Open("user://highscore.data", FileAccess.ModeFlags.Read);

		if (highScoreFile != null)
		{
            highScore = (int)highScoreFile.Get32();
        }
		else
		{
            highScoreFile = FileAccess.Open("user://highscore.data", FileAccess.ModeFlags.Write);
            highScoreFile.Store32((uint)(highScore = 39870));
        }

		highScoreFile.Close();
    }

	private void SaveHighScore()
	{
        FileAccess highScoreFile = FileAccess.Open("user://highscore.data", FileAccess.ModeFlags.Write);
        highScoreFile.Store32((uint)highScore);
		highScoreFile.Close();
    }

    /* RESET */

	private void StopSounds()
	{
		sirenSound.Stop();
		powerPelletSound.Stop();
	}

	private void ResetTriggers()
	{
		foreach (Trigger t in triggers)
		{
			t.Reset(); // disables the trigger and reset the games ticks of the trigger to 0
		}
	}

    private void DisableTriggers()
	{
		foreach (Trigger t in triggers)
		{
			t.Disable();
		}
    }

	private void ResetActors()
	{
        // pacman

        pacman.Visible = true;
        pacman.SetStartState();

        // ghosts

		foreach (Ghost g in ghosts)
		{
			g.Visible = true;
			g.SetStartState();
        }
    }

	private void Reset()
	{
        // reset some control variables

        ticks = 0;
		level = 1;
        score = 0;
		SetFreezeTo(FreezeType.Reset);
        numGhostsEaten = 0;
		numLifes = 3;
		numDotsEaten = 0;
		numDotsEatenThisRound = 0;

		StopSounds();

		// disable triggers & reset actors and maze

		ResetTriggers();
        ResetActors();
        Maze.Reset();

        // reset maze color and show door

        mazeSprite.SelfModulate = new Color("417ae2");
        ghostDoorSprite.Visible = true;

        // start ready trigger

        readyStartedTrigger.Start();
	}

	/* GAME */

	private bool IsFrozen()
	{
		return freeze != (int)FreezeType.None;
	}

	private bool IsFrozenBy(FreezeType freezeType)
	{
		return (freeze & (int)freezeType) == (int)freezeType;
	}

	private void SetFreezeTo(FreezeType freezeType)
	{
		freeze = (int)freezeType;
	}

	private void FreezeBy(FreezeType freezeType)
	{
		freeze |= (int)freezeType;
	}

	private void UnFreeze()
	{
		freeze = (int)FreezeType.None;
	}

	private void UnFreezeBy(FreezeType freezeType)
	{
		freeze &= ~(int)freezeType;
	}

	// init a new round (ready msg only) this occurs when pacman loses a life or at the start of the game

	private void InitRound()
	{
        // disable timers

        DisableTriggers();

        // reset actors

        ResetActors();

		// set freeze to ready

		SetFreezeTo(FreezeType.Ready);

		// check if the last game has been lost or won

		if (numDotsEaten >= Maze.NumDots)
		{
			numDotsEaten = 0;
            level++;
            Maze.Reset();
        }
		else
		{
            numLifes--;
        }

		// reset number of dots eaten this round

		numDotsEatenThisRound = 0;

		// reset maze color and show door

		mazeSprite.SelfModulate = new Color("417ae2");
        ghostDoorSprite.Visible = true;
    }

	/* PACMAN RELATED */

	// check if pacman should move this tick

	private bool PacmanShouldMove()
	{
		if (dotEatenTrigger.IsActive())
		{
			return false;
		}
		else if (pillEatenTrigger.IsActive())
		{
			return false;
		}

		return true;
    }

	/* GHOST RELATED */

    // ghost mode

    private Ghost.Mode GhostScatterChasePhase()
    {
		int s = roundStartedTrigger.TicksSinceStarted();

        if (s < 7 * 60) return Ghost.Mode.Scatter;
        else if (s < 27 * 60) return Ghost.Mode.Chase;
        else if (s < 34 * 60) return Ghost.Mode.Scatter;
        else if (s < 54 * 60) return Ghost.Mode.Chase;
        else if (s < 59 * 60) return Ghost.Mode.Scatter;
        else if (s < 79 * 60) return Ghost.Mode.Chase;
        else if (s < 84 * 60) return Ghost.Mode.Scatter;
        
		return Ghost.Mode.Chase;
    }

    // update the ghost mode

	private bool GhostLeaveHouse(Ghost g)
	{
        switch (g.type)
        {
            case Ghost.Type.Pinky:
                if (numDotsEatenThisRound >= 7)
                {
					return true;
                }
                break;
            case Ghost.Type.Inky:
                if (numDotsEatenThisRound >= 17)
                {
                    return true;
                }
                break;
            case Ghost.Type.Clyde:
                if (numDotsEatenThisRound >= 30)
                {
                    return true;
                }
                break;
        }

		return false;
    }

	private bool IsGhostFrightened(Ghost g)
	{
		return ghostFrightenedTrigger[(int)g.type].IsActive();
    }

	/* ACTORS RELATED (BOTH PACMAN AND GHOSTS) */

	private void UpdateDotsEaten()
	{
		numDotsEaten++;
		numDotsEatenThisRound++;

		// check if there are no dots left

		if (numDotsEaten >= Maze.NumDots)
		{
			roundWonTrigger.Start();
		}

		// spawn fruits

		if (numDotsEaten == 70 || numDotsEaten == 170)
		{
			fruitActiveTrigger.Start();
		}

		// play munch sound

		if ((numDotsEaten & 1) != 0)
		{
			munch1Sound.Play();
		}
		else
		{
			munch2Sound.Play();
		}
	}

	private void UpdateActors()
	{
        /* TICK PACMAN */

        if (PacmanShouldMove())
        {
            pacman.Tick(ticks);
        }

        // Handle dot and pill eating

        Vector2I pacmanTile = pacman.PositionToTile();
		Maze.Tile mazeTile = Maze.GetTile(pacmanTile);

        if (mazeTile == Maze.Tile.Dot || mazeTile == Maze.Tile.Pill)
		{
            switch (mazeTile)
            {
                case Maze.Tile.Dot:
					dotEatenTrigger.Start();

                    // increment score and number of dots eaten

                    score += dotScore;

                    break;
                case Maze.Tile.Pill:
					pillEatenTrigger.Start();

                    // reset num of ghost eaten

                    numGhostsEaten = 0;

                    // set ghosts to be in frightened  mode

                    foreach (Ghost g in ghosts)
                    {
                        if (g.mode == Ghost.Mode.Chase || g.mode == Ghost.Mode.Scatter || g.mode == Ghost.Mode.Frightened)
                        {
							ghostFrightenedTrigger[(int)g.type].Start();
                        }
                    }

                    // increment score and number of dots eaten

                    score += pillScore;

					// play sound

					StopSounds();
					powerPelletSound.Play();

                    break;
            }

            // clear tile, increment number of dots and check if there are no dots left

            Maze.SetTile(pacmanTile, Maze.Tile.Empty);

			UpdateDotsEaten();
        }

		// check if pacman eats fruit

		if (fruitActiveTrigger.IsActive())
		{
			if (pacmanTile == fruitTile)
			{
				fruitActiveTrigger.Disable();
				fruitEatenTrigger.Start();

				// score increment

				score += fruitScores[(int)GetFruitTypeFromLevel(level)];

				// play sound

				fruitSound.Play();
			}
		}

		// check if pacman eats a ghost (or viceversa)

		foreach (Ghost g in ghosts)
		{
			if (pacman.PositionToTile() == g.PositionToTile())
			{
				if (g.mode == Ghost.Mode.Frightened)
				{
                    // ghost has been eaten

                    // freeze the game

                    FreezeBy(FreezeType.EatGhost);

                    // swap ghost mode to eyes

                    g.mode = Ghost.Mode.Eyes;

					// disable frightened trigger

					ghostFrightenedTrigger[(int)g.type].Disable();

					// start ghost eaten and eaten trigger

					ghostEatenUnFreezeTrigger.Start(ghostEatenFreezeTicks);
					ghostEatenTrigger[(int)g.type].Start();

					// increment the score

					score += ghostEatenScores[numGhostsEaten];

					// increment the number of ghosts eaten by one

					numGhostsEaten++;

					// play sound

					ghostEatenSound.Play();
				}
				else if (g.mode == Ghost.Mode.Chase || g.mode == Ghost.Mode.Scatter)
				{
                    // pacman has been eaten

                    // freeze the game

                    FreezeBy(FreezeType.Dead);

                    // start pacman eaten trigger

                    pacmanEatenTrigger.Start(pacmanEatenFreezeTicks);

					// check number of lifes

					if (numLifes >= 1)
					{
                        // start readystarted trigger after (pacmanEatenFreezeTicks + pacmanDeathTicks) ticks

                        readyStartedTrigger.Start(pacmanEatenFreezeTicks + pacmanDeathTicks);
                    }
					else
					{
						// game over

						gameOverTrigger.Start(pacmanEatenFreezeTicks + pacmanDeathTicks);
					}

					// stop sounds

					StopSounds();
				}
			}
		}

        /* TICK GHOSTS */

        foreach (Ghost g in ghosts)
        {
			g.UpdateGhostMode(GhostLeaveHouse, IsGhostFrightened, GhostScatterChasePhase);
			g.UpdateTargetTile(pacman, ghosts);
            g.Tick(ticks);
        }
    }

	private void UpdatePacmanSprite()
	{
        if (IsFrozenBy(FreezeType.EatGhost))
        {
			pacman.Visible = false;
        }
		else if (IsFrozenBy(FreezeType.Dead))
		{
			pacman.Visible = true;

            if (pacmanEatenTrigger.IsActive())
            {
                int tick = pacmanEatenTrigger.TicksSinceStarted();
                pacman.SetDeathSpriteAnimation(tick);
            }
        }
		else if (IsFrozenBy(FreezeType.Ready))
		{
			pacman.Visible = true;
			pacman.SetStartRoundSprite();
		}
		else if (IsFrozenBy(FreezeType.GameOver))
		{
			pacman.Visible = false;
		}
		else
		{
			pacman.Visible = true;
            pacman.SetDefaultSpriteAnimation();
        }
	}

	private void UpdateGhostSprite(Ghost g)
	{
        // check if it has just been eaten

        if (ghostEatenTrigger[(int)g.type].IsActive())
        {
            g.Visible = true;
            g.SetScoreSprite(numGhostsEaten - 1);
        }
        else if (IsFrozenBy(FreezeType.Dead))
        {
            g.Visible = true;

            if (pacmanEatenTrigger.IsActive())
            {
                g.Visible = false;
            }
        }
		else if (IsFrozenBy(FreezeType.Won) || IsFrozenBy(FreezeType.GameOver))
		{
			g.Visible = false;
		}
		else
		{
			g.Visible = true;

            // choose the sprite and animation to show

            switch (g.mode)
            {
                case Ghost.Mode.Frightened:
                    int ticksSinceFrightened = ghostFrightenedTrigger[(int)g.type].TicksSinceStarted();
                    int phase = (ticksSinceFrightened / 4) & 1;
                    g.SetFrightenedSpriteAnimation(phase, ticksSinceFrightened > ghostFrightenedTicks - 60 && (ticksSinceFrightened & 0x10) != 0);
                    break;
                case Ghost.Mode.EnterHouse:
                case Ghost.Mode.Eyes:
                    g.SetEyesSprite();
                    break;
                default:
                    g.SetDefaultSpriteAnimation();
                    break;
            }
        }
    }

	private FruitType GetFruitTypeFromLevel(int levelNumber)
	{
		switch (levelNumber)
		{
			case 1:
				return FruitType.Cherries;
			case 2:
				return FruitType.Strawberry;
            case 3:
			case 4:
				return FruitType.Peach;
            case 5:
            case 6:
                return FruitType.Apple;
            case 7:
            case 8:
                return FruitType.Grapes;
            case 9:
            case 10:
                return FruitType.Galaxian;
            case 11:
            case 12:
                return FruitType.Bell;
            default:
				return FruitType.Key;
        }
	}

	private void UpdateActorsSprites()
	{
		// pacman

		UpdatePacmanSprite();

		// ghosts

		foreach (Ghost g in ghosts)
		{
			UpdateGhostSprite(g);
		}
	}

	// score update display

	private void UpdateScore()
	{
        scoreText.Text = (score == 0) ? "00" : score.ToString();

        if (score > highScore)
        {
            highScore = score;
        }

        highScoreText.Text = "HIGH SCORE\n" + ((highScore == 0) ? "00" : highScore.ToString());
    }

	/* DEBUG */

	private void DrawGhostsPaths()
	{
        Color[] pathColors = new Color[4] { Color.Color8(255, 0, 0, 255), Color.Color8(252, 181, 255, 255), Color.Color8(0, 255, 255, 255), Color.Color8(248, 187, 85, 255) };
		int pathLineWidth = 2;

		for (int i = 0; i < 4; i++)
		{
			List<Vector2I> path = ghostsPaths[i];

			if (path.Count > 0)
			{
				for (int j = 0; j < path.Count - 1; j++)
				{
					Vector2I p1 = path[j];
					Vector2I p2 = path[j + 1];
					Vector2I pathDirection = p2 - p1;

					Vector2I pathLineSize = Vector2I.Zero;

					switch (pathDirection.X)
					{
						case 0:
							pathLineSize.X = pathLineWidth;
							break;
						case 1:
                            pathLineSize.X = 8 + pathLineWidth;
                            break;
						case -1:
                            pathLineSize.X = -8;
                            break;
					}

                    switch (pathDirection.Y)
                    {
                        case 0:
                            pathLineSize.Y = pathLineWidth;
                            break;
                        case 1:
                            pathLineSize.Y = 8 + pathLineWidth;
                            break;
                        case -1:
                            pathLineSize.Y = -8;
                            break;
                    }

					DrawRect(new Rect2I(p1 * 8 + new Vector2I(3, 3), pathLineSize), pathColors[i]);
                }

				DrawRect(new Rect2I(path[path.Count - 1] * 8 + Vector2I.One * ((8 - pathLineWidth * 2) >> 1), new Vector2I(pathLineWidth, pathLineWidth) * 2), pathColors[i]);
			}
		}
	}

	private void CalculateGhostsPaths()
	{
		for (int i = 0; i < 4; i++)
		{
			if (ghosts[i].DistanceToTileMid() == Vector2I.Zero)
			{
				ghosts[i].GetCurrentPath(ghostsPaths[i], 17);
			}
		}
	}

    // Called when the node enters the scene tree for the first time.

    public override void _Ready()
	{
        // create triggers

        triggers.Add(dotEatenTrigger = new Trigger());
        triggers.Add(pillEatenTrigger = new Trigger(3));
		triggers.Add(readyStartedTrigger = new Trigger(Callable.From(() =>
		{
			InitRound();
			roundStartedTrigger.Start(2 * 60);
        })));
		triggers.Add(roundStartedTrigger = new Trigger(Callable.From(() =>
		{
			UnFreeze();

			StopSounds();
			sirenSound.Play();
        })));
		triggers.Add(roundWonTrigger = new Trigger(Callable.From(() =>
		{
            FreezeBy(FreezeType.Won);
            readyStartedTrigger.Start(roundWonFreezeTicks);

			StopSounds();
        })));
		triggers.Add(gameOverTrigger = new Trigger(Callable.From(() =>
		{
			DisableTriggers();
            SetFreezeTo(FreezeType.GameOver);
            StopSounds();

			if (score >= highScore)
			{
                SaveHighScore();
            }

            resetTrigger.Start(3 * 60);
		})));
		triggers.Add(resetTrigger = new Trigger(Callable.From(() =>
		{
			Reset();
		})));
		triggers.Add(fruitActiveTrigger = new Trigger(fruitActiveTicks));
        triggers.Add(fruitEatenTrigger = new Trigger(2 * 60)); // show fruit score for 2 secs
		triggers.Add(pacmanEatenTrigger = new Trigger(pacmanDeathTicks));
		triggers.Add(ghostEatenUnFreezeTrigger = new Trigger(Callable.From(() =>
		{
			UnFreezeBy(FreezeType.EatGhost);
		})));

		for (int i = 0; i < 4; i++)
		{
			triggers.Add(ghostFrightenedTrigger[i] = new Trigger(ghostFrightenedTicks));
			triggers.Add(ghostEatenTrigger[i] = new Trigger(ghostEatenFreezeTicks));
		}

        // get nodes

        scoreText = GetNode<Label>("Score");
		highScoreText = GetNode<Label>("HighScore");
		mazeSprite = GetNode<Sprite2D>("Maze");
		ghostDoorSprite = GetNode<ColorRect>("GhostDoor");

		munch1Sound = GetNode<AudioStreamPlayer>("Munch1Sound");
        munch2Sound = GetNode<AudioStreamPlayer>("Munch2Sound");
		fruitSound = GetNode<AudioStreamPlayer>("FruitSound");
		ghostEatenSound = GetNode<AudioStreamPlayer>("GhostEatenSound");
		sirenSound = GetNode<AudioStreamPlayer>("SirenSound");
		powerPelletSound = GetNode<AudioStreamPlayer>("PowerPelletSound");

        // create pacman

        pacman = (Pacman)pacmanScene.Instantiate();
		AddChild(pacman);

		// create ghosts

		for (int i = 0; i < 4; i++)
		{
			ghosts[i] = (Ghost)ghostScene.Instantiate();
			ghosts[i].type = (Ghost.Type)i;
			AddChild(ghosts[i]);
		}

		// ghost paths

		for (int i = 0; i < 4; i++)
		{
			ghostsPaths[i] = new List<Vector2I>();
		}

		// reset state & set high score

		LoadHighScore();
        Reset();

		// hide mouse cursor

		DisplayServer.MouseSetMode(DisplayServer.MouseMode.Hidden);
    }

    // draw (for debug)

    public override void _Draw()
	{
		// draw ghost paths

		// DrawGhostsPaths();

		// draw dots and pills

		for (int j = 0; j < Maze.Height; j++)
        {
            for (int i = 0; i < Maze.Width; i++)
            {
                Rect2 dotRect = new Rect2(new Vector2(i * Maze.TileSize, j * Maze.TileSize), new Vector2(Maze.TileSize, Maze.TileSize));

                switch (Maze.GetTile(new Vector2I(i, j)))
                {
                    case Maze.Tile.Dot:
                        DrawTextureRectRegion(dotsTexture, dotRect, new Rect2(Vector2.Zero, new Vector2(Maze.TileSize, Maze.TileSize)));
                        break;
                    case Maze.Tile.Pill:
						if ((ticks & 8) != 0 || freeze != 0)
						{
							DrawTextureRectRegion(dotsTexture, dotRect, new Rect2(new Vector2(Maze.TileSize, 0), new Vector2(Maze.TileSize, Maze.TileSize)));
						}
                        break;
                }
            }
        }

		// draw ready text

		if (IsFrozenBy(FreezeType.Ready))
		{
			DrawTexture(readyTextTexture, new Vector2I(89, 131));
		}

		// draw game over text

		if (IsFrozenBy(FreezeType.GameOver))
		{
			DrawTexture(gameOverTextTexture, new Vector2I(73, 131));
		}

        // maze animation when round won

        if (IsFrozenBy(FreezeType.Won))
        {
			int ticksSinceWon = roundWonTrigger.TicksSinceStarted();
            mazeSprite.SelfModulate = (ticksSinceWon & 16) != 0 ? new Color("417ae2") : new Color("ffffff");
            ghostDoorSprite.Visible = false;
        }

        // draw lifes

        for (int i = 0; i < numLifes; i++)
		{
			DrawTexture(lifeTexture, new Vector2I(16 + 16 * i, 248));
		}

		// draw the fruits that represent the level number

		int levelStart = level - 7 > 0 ? level - 7 : 0;

		for (int i = levelStart; i < level; i++)
		{
			int fruitIndex = (int)GetFruitTypeFromLevel(i + 1);
			DrawTextureRectRegion(fruitTexture, new Rect2I(new Vector2I(188 - 16 * (i - levelStart), 248), new Vector2I(24, 16)), new Rect2I(new Vector2I(0, fruitIndex * 16), new Vector2I(24, 16)));
		}

		// draw fruit

		if (fruitActiveTrigger.IsActive())
		{
            int fruitIndex = (int)GetFruitTypeFromLevel(level);
            DrawTextureRectRegion(fruitTexture, new Rect2I(new Vector2I(100, 132), new Vector2I(24, 16)), new Rect2I(new Vector2I(0, fruitIndex * 16), new Vector2I(24, 16)));
		}
		else if (fruitEatenTrigger.IsActive())
		{
            int fruitIndex = (int)GetFruitTypeFromLevel(level);
            DrawTextureRectRegion(fruitTexture, new Rect2I(new Vector2I(100, 132), new Vector2I(24, 16)), new Rect2I(new Vector2I(24, fruitIndex * 16), new Vector2I(24, 16)));
        }
    }

    // runs at 60 fps

    public override void _PhysicsProcess(double delta)
	{
		// toggle fullscreen

		if (Input.IsActionJustPressed("ToggleFullscreen"))
		{
			Window window = GetWindow();

			if (window.Mode != Window.ModeEnum.ExclusiveFullscreen)
			{
				window.Mode = Window.ModeEnum.ExclusiveFullscreen;
			}
			else
			{
				window.Mode = Window.ModeEnum.Windowed;
            }
        }

        // reset

        if (Input.IsActionJustPressed("Reset"))
		{
			Reset();
		}

        // update triggers

        foreach (Trigger t in triggers)
        {
            t.Tick(ticks);
        }

		// sound change from power pellet back to siren

        if (powerPelletSound.Playing)
        {
			bool changeToSiren = true;

			foreach (Ghost g in ghosts)
			{
				if (IsGhostFrightened(g))
				{
					changeToSiren = false;
                    break;
				}
			}

			if (changeToSiren)
			{
                StopSounds();
                sirenSound.Play();
            }
        }

        // update actors if the game is not frozen

        if (!IsFrozen())
		{
			UpdateActors();
		}

        // update score

        UpdateScore();

		// update sprites

        UpdateActorsSprites();

		// debug ghost paths

		// CalculateGhostsPaths();

        // redraw

        QueueRedraw();

        // increment number of ticks

        ticks++;
    }
}
