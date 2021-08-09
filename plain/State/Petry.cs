using System;
using System.Collections.Generic;
using System.Linq;

using Lexxys;

namespace State
{
	class Petry
	{
		public IEnumerable<Room> Rooms { get; private set; }

		public Room Current { get; private set; }


		public bool Next()
		{
			Room room = Current;
			bool moved = room.Balls.ToList().Aggregate(false, (x, o) => x | room.Doors.Select(p => p.GoThrough(o)).Any(p => p != null));
			if (moved)
				LastMoved = room;
			return moved;
		}

		public Room LastMoved { get; private set; }
	}

	class Ball
	{

	}

	class Room
	{
		public IEnumerable<Ball> Balls { get; private set; }
		public IEnumerable<Door> Doors { get; private set; }
	}

	class Door
	{
		public Room GoThrough(Ball ball)
		{
			throw new NotImplementedException();
		}
	}

}