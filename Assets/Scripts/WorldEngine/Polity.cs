﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class PolityInfluence {

	[XmlAttribute]
	public long PolityId;
	[XmlAttribute]
	public float Value;

	[XmlIgnore]
	public Polity Polity;

	public PolityInfluence (Polity polity, float value) {
	
		PolityId = polity.Id;
		Polity = polity;
		Value = value;
	}
}

public abstract class Polity : Synchronizable {

	public const float MinPolityInfluence = 0.001f;

	[XmlAttribute]
	public long Id;

	public long CoreGroupId;

	public List<long> InfluencedGroupIds;

	public Territory Territory = new Territory ();

	public PolityCulture Culture;

	[XmlIgnore]
	public World World;

	[XmlIgnore]
	public CellGroup CoreGroup;

	private HashSet<CellGroup> _influencedGroups = new HashSet<CellGroup> ();

	public Polity () {
	
	}

	public Polity (CellGroup coreGroup, float coreGroupInfluence) {

		World = coreGroup.World;

		Id = World.GeneratePolityId ();

		AddInfluencedGroup (coreGroup);

		SetCoreGroup (coreGroup);

		Culture = new PolityCulture (this);

		coreGroup.SetPolityInfluenceValue (this, coreGroupInfluence);
	}

	public void SetCoreGroup (CellGroup group) {

		if (!_influencedGroups.Contains (group))
			throw new System.Exception ("Group is not part of polity's influenced groups");

		CoreGroup = group;

		CoreGroupId = group.Id;
	}

	public void AddInfluencedGroup (CellGroup group) {
	
		_influencedGroups.Add (group);

		Territory.AddCell (group.Cell);
	}

	public void RemoveInfluencedGroup (CellGroup group) {

		_influencedGroups.Remove (group);

		Territory.RemoveCell (group.Cell);
	}

	public virtual void Synchronize () {

		Culture.Synchronize ();

		InfluencedGroupIds = new List<long> (_influencedGroups.Count);

		foreach (CellGroup g in _influencedGroups) {

			InfluencedGroupIds.Add (g.Id);
		}
	}

	public virtual void FinalizeLoad () {

		CoreGroup = World.GetGroup (CoreGroupId);

		if (CoreGroup == null) {
			throw new System.Exception ("Missing Group with Id " + CoreGroupId);
		}

		foreach (int id in InfluencedGroupIds) {

			CellGroup group = World.GetGroup (id);

			if (group == null) {
				throw new System.Exception ("Missing Group with Id " + id);
			}

			_influencedGroups.Add (group);
		}

		Culture.Polity = this;
		Culture.FinalizeLoad ();
	}

	public abstract float MigrationValue (TerrainCell targetCell, float sourceRelativeInfluence);
	public abstract void MergingEffects (CellGroup targetGroup, float sourceInfluence, float percentOfTarget);
	public abstract void UpdateEffects (CellGroup group, float influence, int timeSpan);
}

public class Territory {

	public List<WorldPosition> CellPositions = new List<WorldPosition> ();

	[XmlIgnore]
	public World World;

	private HashSet<TerrainCell> _cells = new HashSet<TerrainCell> ();

	public Territory () {
	
	}

	public Territory (World world) {

		World = world;
	}

	public bool AddCell (TerrainCell cell) {

		if (!_cells.Add (cell))
			return false;

		CellPositions.Add (cell.Position);

		cell.AddEncompassingTerritory (this);

		return true;
	}

	public bool RemoveCell (TerrainCell cell) {

		if (!_cells.Remove (cell))
			return false;

		CellPositions.Remove (cell.Position);

		cell.RemoveEncompassingTerritory (this);

		return true;
	}

	public void FinalizeLoad () {

		foreach (WorldPosition position in CellPositions) {

			TerrainCell cell = World.GetCell (position);

			if (cell == null) {
				throw new System.Exception ("Cell missing at position " + position.Longitude + "," + position.Latitude);
			}
		
			_cells.Add (cell);

			cell.AddEncompassingTerritory (this);
		}
	}
}
