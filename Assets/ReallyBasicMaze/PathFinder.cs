using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Runtime.CompilerServices;

public struct CellPriorityQueue : INativeDisposable
{
	public static CellPriorityQueue Null { get; set; }
	private NativeList<int> list;
	public NativeArray<CellQueueElement> elements;
	private int count;
	private int minimum;
	public int searchPhase;
	private Allocator allocatedWith;
	public Allocator AllocatedWith { get { return allocatedWith; } }

	public CellPriorityQueue(NativeArray<CellQueueElement> cellsIn, Allocator allocator = Allocator.Temp)
	{
		elements = cellsIn;
		list = new NativeList<int>(cellsIn.Length, allocator);
		count = 0;
		minimum = int.MaxValue;
		searchPhase = 0;
		allocatedWith = allocator;
	}

	public void Reallocate(Allocator As)
	{
		NativeArray<int> temp = list.ToArray(Allocator.Temp);
		NativeArray<CellQueueElement> elementsTemp = new NativeArray<CellQueueElement>(elements, Allocator.Temp);
		list.Dispose();
		elements.Dispose();
		list = new NativeList<int>(temp.Length, As);
		list.CopyFrom(temp);
		elements = new NativeArray<CellQueueElement>(elementsTemp, As);
		temp.Dispose();
		elementsTemp.Dispose();
		allocatedWith = As;
	}

	public void SetElements(NativeArray<CellQueueElement> cellsIn, Allocator allocator = Allocator.Temp)
	{
		try
		{
			elements.Dispose();
		}
		catch { }
		try
		{
			list.Dispose();
		}
		catch { }
		elements = cellsIn;
		list = new NativeList<int>(cellsIn.Length, allocator);
	}

	public int Count
	{
		get
		{
			return count;
		}
	}

	public void Enqueue(CellQueueElement cell)
	{
		count += 1;
		int priority = cell.SearchPriority;
		if (priority < minimum)
		{
			minimum = priority;
		}
		if (priority > list.Capacity)
		{
			list.Capacity = priority + 1;
		}
		while (priority >= list.Length)
		{
			list.Add(int.MinValue);
		}

		cell.NextWithSamePriority = list[priority];
		elements[cell.cellIndex] = cell;
		list[priority] = cell.cellIndex;
	}

	public int Dequeue()
	{
		count -= 1;
		for (; minimum < list.Length; minimum++)
		{
			int potentialCell = list[minimum];
			if (potentialCell != int.MinValue)
			{
				list[minimum] = elements[potentialCell].NextWithSamePriority;
				return potentialCell;
			}
		}
		return int.MinValue;
	}

	public void Change(CellQueueElement cell, int oldPriority)
	{
		elements[cell.cellIndex] = cell;
		int current = list[oldPriority];
		int next = elements[current].NextWithSamePriority;

		if (current == cell.cellIndex)
		{
			list[oldPriority] = next;
		}
		else
		{
			while (next != cell.cellIndex)
			{
				current = next;
				next = elements[current].NextWithSamePriority;
			}
			CellQueueElement currentElement = elements[current];
			currentElement.NextWithSamePriority = cell.NextWithSamePriority;
			elements[current] = currentElement;
			Enqueue(cell);
			count -= 1;
		}
	}

	public void Clear()
	{
		list.Clear();
		count = 0;
		minimum = int.MaxValue;
	}

	public JobHandle Dispose(JobHandle inputDeps)
	{
		return elements.Dispose(list.Dispose(inputDeps));
	}

	public void Dispose()
	{
		elements.Dispose();
		list.Dispose();
	}
}

public struct CellQueueElement
{
	public int cellIndex;
	public int NextWithSamePriority;
	public int SearchPhase;
	public int Distance;
	public int SearchHeuristic;
	public int SearchPriority { get { return Distance + SearchHeuristic; } }
}

public enum CellDirection
{
	N,
	E,
	S,
	W
}

public static class CellDirectionExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CellDirection Opposite(this CellDirection direction)
	{
		return (int)direction < 2 ? (direction + 2) : (direction - 2);
	}
}

public struct Cell : IBufferElementData, System.IEquatable<Cell>
{
	public static readonly Cell Null = Create(int.MinValue, int.MinValue, int.MinValue);

	public int Index;
	public int x;
	public int z;
	public int PathFrom;

	public int NeighbourN;
	public int NeighbourE;
	public int NeighbourS;
	public int NeighbourW;

	public bool SetWall;
	public Vector3 UsablePosition;
	public bool HasFloor;

	public static Cell Create(int Index, int x, int z)
	{
		return new Cell
		{
			Index = Index,
			x = x,
			z = z,
			NeighbourN = int.MinValue,
			NeighbourE = int.MinValue,
			NeighbourS = int.MinValue,
			NeighbourW = int.MinValue,
			SetWall = false,
			UsablePosition = Vector3.zero,
			HasFloor = false,
			PathFrom = int.MinValue
		};
	}

	public static NativeArray<Cell> CreateGird(int cellCountZ, int cellCountX, Allocator allocator = Allocator.Persistent)
	{
		// 1
		NativeArray<Cell> cells = new NativeArray<Cell>(cellCountZ * cellCountX, allocator);
		for (int i = 0, z = 0; z < cellCountZ; z++)
		{
			for (int x = 0; x < cellCountX; x++)
			{
				cells[i] = Create(i++, x, z);
			}
		}
		// 2
		for (int i = 0; i < cells.Length; i++)
		{
			Cell cell = cells[i];
			int x = cell.x;
			int z = cell.z;
			switch (x > 0)
			{
				case true:
					cell = SetNeighbour(cell, CellDirection.W, i - 1);
					break;
			}
			switch (z > 0)
			{
				case true:
					cell = SetNeighbour(cell, CellDirection.S, i - cellCountX);
					break;
			}
			cells[i] = cell;
		}
		// 3
		for (int i = 0; i < cells.Length; i++)
		{
			Cell cell = cells[i];
			for (CellDirection d = CellDirection.N; d <= CellDirection.W; d++)
			{
				Cell neighbour = GetNeighbour(cell, cells, d);
				switch (neighbour.Equals(Null))
				{
					case false:
						neighbour = SetNeighbour(neighbour, d.Opposite(), i);
						cells[neighbour.Index] = neighbour;
						break;
				}
			}
			cells[i] = cell;
		}

		return cells;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Cell SetNeighbour(Cell cell, CellDirection direction, int neighbourIndex)
	{
		switch (direction)
		{
			case CellDirection.N:
				cell.NeighbourN = neighbourIndex;
				break;
			case CellDirection.E:
				cell.NeighbourE = neighbourIndex;
				break;
			case CellDirection.S:
				cell.NeighbourS = neighbourIndex;
				break;
			case CellDirection.W:
				cell.NeighbourW = neighbourIndex;
				break;
		}
		return cell;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Cell GetNeighbour(Cell cell, NativeArray<Cell> cells, CellDirection direction)
	{
		int neighbourIndex = direction switch
		{
			CellDirection.N => cell.NeighbourN,
			CellDirection.E => cell.NeighbourE,
			CellDirection.S => cell.NeighbourS,
			CellDirection.W => cell.NeighbourW,
			_ => int.MinValue,
		};

		return (neighbourIndex == int.MinValue) switch
		{
			true => Null,
			false => cells[neighbourIndex]

		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Cell other)
	{
		return this.Index == other.Index;
	}
}