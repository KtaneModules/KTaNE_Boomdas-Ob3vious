using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class boomdasScript : MonoBehaviour {

	public KMAudio Audio;
	public AudioClip[] sounds;
	public KMBombInfo BombInfo;
	public GameObject PieceParent;
	public GameObject[] Piece;
	public KMSelectable[] Button;
	public KMSelectable Submit;
	public KMBombModule Module;

	//[0] = x; [1] = y
	private int[][] squarepos;
	private int[][] graph;
	private int[][] sbmt;
	private bool solved = false;
	private bool[] movingsquares = { false, false, false, false, false, false, false, false, false };

	static int _moduleIdCounter = 1;
	int _moduleID = 0;

	private KMSelectable.OnInteractHandler ButtonPressed(int pos)
	{
		return delegate
		{
			Button[pos].AddInteractionPunch();
			if (!solved)
			{
				Debug.Log("<---New move--->");
				movingsquares = new bool[] { false, false, false, false, false, false, false, false, false };
				MoveSquare(pos / 4, pos % 4);
				Debug.Log("<---End move--->");
				StartCoroutine(Slide(pos % 4));
			}
            else
            {
				Audio.PlaySoundAtTransform("Click", Module.transform);
			}
			return false;
		};
	}

	private KMSelectable.OnInteractHandler SubmitPressed()
	{
		return delegate
		{
			Submit.AddInteractionPunch();
			Audio.PlaySoundAtTransform("Click", Module.transform);
			if (!solved)
			{
				int[] state = { -1, -1, -1, -1, -1, -1, -1, -1, -1 };
				int statepos = 0;
				bool solving = true;
				sbmt = new int[][] { new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 } };
				for (int i = 0; i < 9; i++)
				{
					sbmt[squarepos[1].Max() - squarepos[1][i]][squarepos[0][i] - squarepos[0].Min()] = i;
				}
				for (int i = 0; i < 8; i++)
				{
					for (int j = 0; j < 8; j++)
					{
						if ((sbmt[j][i] == -1) != (graph[j][i] == -1))
						{
							solving = false;
						}
						if (sbmt[i][j] != -1)
						{
							state[statepos] = sbmt[i][j];
							statepos++;
						}
					}
				}
				int[] testArray = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
				if (solving || (graph[0] == testArray))
				{
					solved = true;
					Module.HandlePass();
				}
				else
				{
					Debug.LogFormat("[Boomdas #{0}] You submitted:", _moduleID);
                    for (int i = 0; i < 8; i++)
                    {
						Debug.LogFormat("[Boomdas #{0}] {1}", _moduleID, sbmt[i].Join("").Replace("-1", "_").Replace("0", "1").Replace("1", "2").Replace("2", "3").Replace("3", "4").Replace("4", "5").Replace("5", "6").Replace("6", "7").Replace("7", "8").Replace("8", "X"));
					}
					Debug.LogFormat("[Boomdas #{0}] which is incorrect", _moduleID);
					Module.HandleStrike();
					Debug.LogFormat("[Boomdas #{0}] Order is: {1}.", _moduleID, state.Select(x => x + 1).Join(", "));
					Solution(state);
				}
			}
			return false;
		};
	}

	// Use this for initialization
	void Awake () {
		_moduleID = _moduleIdCounter++;
		squarepos = new int[][] { new int[] { -1, 0, 1, -1, 0, 1, -1, 0, 1 }, new int[] { 1, 1, 1, 0, 0, 0, -1, -1, -1 } };
		int[] state = { -1, -1, -1, -1, -1, -1, -1, -1, -1 };
		int[] valid = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
		for (int i = 0; i < state.Length; i++)
		{
			state[i] = valid.Where(x => !state.Contains(x)).ToArray()[Rnd.Range(0, valid.Where(x => !state.Contains(x)).ToArray().Length)];
			Piece[state[i]].transform.localPosition = new Vector3(squarepos[0][i] * 0.02f, 0.015f, squarepos[1][i] * 0.02f);
		}
		for (int i = 0; i < 9; i++)
		{
			squarepos[0][state[i]] = i % 3 - 1;
			squarepos[1][state[i]] = 1 - i / 3;
		}
		Debug.LogFormat("[Boomdas #{0}] Order is: {1}.", _moduleID, state.Select(x => x + 1).Join(", "));
		Solution(state);
		for (int i = 0; i < Button.Length; i++)
		{
			Button[i].OnInteract += ButtonPressed(i);
		}
		Submit.OnInteract += SubmitPressed();
	}

	void MoveSquare(int button, int direction)
	{
		int[] dirtransfx = { 0, 0, -1, 1 };
		int[] dirtransfy = { 1, -1, 0, 0 };
		movingsquares[button] = true;
		squarepos[0][button] += dirtransfx[direction]; squarepos[1][button] += dirtransfy[direction];
		Debug.Log("Moving " + (button + 1));
		for (int i = 0; i < 9; i++)
		{
			//pushing
			if ((squarepos[0][i] == squarepos[0][button]) && (squarepos[1][i] == squarepos[1][button]) && !movingsquares[i])
			{
				MoveSquare(i, direction);
			}
			//pulling backside
			else if ((squarepos[0][i] == squarepos[0][button] - 2 * dirtransfx[direction]) && (squarepos[1][i] == squarepos[1][button] - 2 * dirtransfy[direction]) && !movingsquares[i])
			{
				bool move = true;
				bool move2 = true;
				bool move3 = true;
				bool move4 = true;
				for (int j = 0; j < 9; j++)
				{
					for (int k = 0; k < 9; k++)
					{
						if (((squarepos[0][j] == squarepos[0][button] + dirtransfx[(direction + 2) % 4] || squarepos[0][j] == squarepos[0][button] - dirtransfx[(direction + 2) % 4]) && (squarepos[1][j] == squarepos[1][button] + dirtransfy[(direction + 2) % 4] || squarepos[1][j] == squarepos[1][button] - dirtransfy[(direction + 2) % 4])) && (squarepos[0][k] == squarepos[0][j] - dirtransfx[direction]) && (squarepos[1][k] == squarepos[1][j] - dirtransfy[direction]))
						{
							move = false;
						}
						if (((squarepos[0][j] == squarepos[0][i] + dirtransfx[(direction + 2) % 4] || squarepos[0][j] == squarepos[0][i] - dirtransfx[(direction + 2) % 4]) && (squarepos[1][j] == squarepos[1][i] + dirtransfy[(direction + 2) % 4] || squarepos[1][j] == squarepos[1][i] - dirtransfy[(direction + 2) % 4])) && (squarepos[0][k] == squarepos[0][j] + dirtransfx[direction]) && (squarepos[1][k] == squarepos[1][j] + dirtransfy[direction]))
						{
							move2 = false;
						}
						if ((squarepos[0][j] == squarepos[0][button] + dirtransfx[(direction + 2) % 4] && squarepos[1][j] == squarepos[1][button] + dirtransfy[(direction + 2) % 4]) && (squarepos[0][k] == squarepos[0][button] - dirtransfx[(direction + 2) % 4] - 2 * dirtransfx[direction] && squarepos[1][k] == squarepos[1][button] - dirtransfy[(direction + 2) % 4] - 2 * dirtransfy[direction]))
						{
							move3 = false;
						}
						if ((squarepos[0][j] == squarepos[0][button] - dirtransfx[(direction + 2) % 4] && squarepos[1][j] == squarepos[1][button] - dirtransfy[(direction + 2) % 4]) && (squarepos[0][k] == squarepos[0][button] + dirtransfx[(direction + 2) % 4] - 2 * dirtransfx[direction] && squarepos[1][k] == squarepos[1][button] + dirtransfy[(direction + 2) % 4] - 2 * dirtransfy[direction]))
						{
							move4 = false;
						}
					}
				}
				if (move || move2 || ((move3 ^ move4) && !(move || move2)))
					MoveSquare(i, direction);
			}
			//pulling sideways
			else if ((squarepos[0][i] + dirtransfx[direction] == squarepos[0][button] + dirtransfx[(direction + 2) % 4] || squarepos[0][i] + dirtransfx[direction] == squarepos[0][button] - dirtransfx[(direction + 2) % 4]) && (squarepos[1][i] + dirtransfy[direction] == squarepos[1][button] + dirtransfy[(direction + 2) % 4] || squarepos[1][i] + dirtransfy[direction] == squarepos[1][button] - dirtransfy[(direction + 2) % 4]) && !movingsquares[i])
			{
				bool move = true;
				bool move2 = true;
				bool move3 = false;
				int shift3 = 0;
				for (int j = 0; j < 9; j++)
				{
					if (squarepos[0][i] + dirtransfx[direction] == squarepos[0][j] && squarepos[1][i] + dirtransfy[direction] == squarepos[1][j])
					{
						move = false;
					}
					if (((squarepos[0][j] == squarepos[0][button] - 2 * dirtransfx[direction]) && (squarepos[1][j] == squarepos[1][button] - 2 * dirtransfy[direction]) && !movingsquares[j]) || ((squarepos[0][j] == squarepos[0][button] - dirtransfx[direction]) && (squarepos[1][j] == squarepos[1][button] - dirtransfy[direction]) && movingsquares[j]))
					{
						move2 = false;
						if ((squarepos[0][j] == squarepos[0][button] - 2 * dirtransfx[direction]) && (squarepos[1][j] == squarepos[1][button] - 2 * dirtransfy[direction]) && !movingsquares[j])
                        {
							move3 = true;
							shift3 = j;
						}
					}
				}
				if (move && move2)
				{
					MoveSquare(i, direction);
				}
                else if (move && move3)
                {
					MoveSquare(shift3, direction);
				}
			}
		}
	}

	private void Solution(int[] order)
	{
		graph = new int[][] { new int[] { 0, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 } };
		int n = 0;
		int test = 0;
		for (int i = 0; i < 8; i++)
		{
			n = n * 10 + (order[i] + 1);
			int pos = (n / 4) % (i + 1);
			int dir = n % 4;
			for (int j = 0; j < 8; j++)
			{
				for (int k = 0; k < 8; k++)
				{
					if (graph[j][k] == pos)
					{
						switch (dir)
						{
							case 0:
								test = test | 1;
								while (true)
								{
									if (j == -1)
									{
										for (int l = 6; l >= 0; l--)
										{
											for (int m = 0; m < 8; m++)
											{
												graph[l + 1][m] = graph[l][m];
											}
										}
										graph[0] = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
										graph[0][k] = (i + 1);
										j = 8;
										k = 8;
										break;
									}
									else
									{
										if (graph[j][k] == -1)
										{
											graph[j][k] = (i + 1);
											j = 8;
											k = 8;
											break;
										}
										else
										{
											j--;
										}
									}
								}
								break;
							case 1:
								test = test | 2;
								while (true)
								{
									if (k == 8)
									{
										break;
									}
									else
									{
										if (graph[j][k] == -1)
										{
											graph[j][k] = (i + 1);
											j = 8;
											k = 8;
											break;
										}
										else
										{
											k++;
										}
									}
								}
								break;
							case 2:
								test = test | 1;
								while (true)
								{
									if (j == 8)
									{
										break;
									}
									else
									{
										if (graph[j][k] == -1)
										{
											graph[j][k] = (i + 1);
											j = 8;
											k = 8;
											break;
										}
										else
										{
											j++;
										}
									}
								}
								break;
							case 3:
								test = test | 2;
								while (true)
								{
									if (k == -1)
									{
										for (int l = 6; l >= 0; l--)
										{
											for (int m = 0; m < 8; m++)
											{
												graph[m][l + 1] = graph[m][l];
											}
										}
										for (int l = 0; l < 8; l++)
										{
											graph[l][0] = -1;
										}
										graph[j][0] = (i + 1);
										j = 8;
										k = 8;
										break;
									}
									else
									{
										if (graph[j][k] == -1)
										{
											graph[j][k] = (i + 1);
											j = 8;
											k = 8;
											break;
										}
										else
										{
											k--;
										}
									}
								}
								break;
						}
					}
				}
			}
		}
		if (test != 3)
		{
			graph = new int[][] { new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new int[] { -1, -1, -1, -1, -1, -1, -1, -1 } };
			Debug.LogFormat("[Boomdas #{0}] Looks like you got the unicorn!", _moduleID);
		}
		else
		{
			Debug.LogFormat("[Boomdas #{0}] Expecting:", _moduleID);
			for (int i = 0; i < 8; i++)
			{
				Debug.LogFormat("[Boomdas #{0}] {1}", _moduleID, graph[i].Join("").Replace("-1", "_").Replace("0", "1").Replace("1", "2").Replace("2", "3").Replace("3", "4").Replace("4", "5").Replace("5", "6").Replace("6", "7").Replace("7", "8").Replace("8", "X"));
			}
		}
	}

	private IEnumerator Slide(int dir)
	{
		Audio.PlaySoundAtTransform("Click", Module.transform);
		float factor = 0.02f;
		int[] dirtransfx = { 0, 0, -1, 1 };
		int[] dirtransfy = { 1, -1, 0, 0 };
		for (int t = 1; t <= 20; t++)
		{
			yield return new WaitForSeconds(0.01f);
			Vector3 parent = new Vector3(0f, 0f, 0f);
			for (int i = 0; i < 9; i++)
			{
				if (movingsquares[i])
				{
					Piece[i].transform.localPosition = Vector3.Lerp(new Vector3((squarepos[0][i] - dirtransfx[dir]) * factor, 0.015f, (squarepos[1][i] - dirtransfy[dir]) * factor), new Vector3(squarepos[0][i] * factor, 0.015f, squarepos[1][i] * factor), (t / 20f));
				}
                else
                {
					Piece[i].transform.localPosition = new Vector3((squarepos[0][i]) * factor, 0.015f, (squarepos[1][i]) * factor);
				}
				parent += Piece[i].transform.localPosition / 9;
			}
			PieceParent.transform.localPosition = new Vector3(-parent.x, 0f, -parent.z);
		}
		Audio.PlaySoundAtTransform("Click", Module.transform);
	}

#pragma warning disable 414
	private string TwitchHelpMessage = "'!{0} submit' to submit the configuration. '!{0} 5u/d/l/r' to move piece 5 (and other necessary pieces) up/left/down/right. Commands can be chained. e.g. '!{0} 5u 6l 4r 1d'";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
		command = command.ToLowerInvariant();
		if (command == "submit") { Submit.OnInteract(); yield return "strike"; yield return "solve"; }
		else
		{
			string[] cmds = command.Split(' ');
			string[] valid = { "1u", "1d", "1l", "1r", "2u", "2d", "2l", "2r", "3u", "3d", "3l", "3r", "4u", "4d", "4l", "4r", "5u", "5d", "5l", "5r", "6u", "6d", "6l", "6r", "7u", "7d", "7l", "7r", "8u", "8d", "8l", "8r", "9u", "9d", "9l", "9r" };
			for (int i = 0; i < cmds.Length; i++)
			{
				if (!valid.Contains(cmds[i]))
				{
					yield return "sendtochaterror Invalid command.";
					yield break;
				}
				else
				{
					for (int j = 0; j < 36; j++)
					{
						if (valid[j] == cmds[i])
						{
							Button[j].OnInteract();
							yield return new WaitForSeconds(0.40f);
						}
					}
				}
			}
		}
		yield return null;
	}
}
