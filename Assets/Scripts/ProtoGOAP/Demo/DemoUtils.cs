﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoGOAP.Demo
{
	public static class DemoUtils
	{
		public static GameObject FindClosestWithTag(Vector3 position, string tag)
		{
			GameObject[] gos = GameObject.FindGameObjectsWithTag(tag);
			GameObject closest = null;
			float distance = Mathf.Infinity;
			foreach (GameObject go in gos) {
				Vector3 diff = go.transform.position - position;
				float curDistance = diff.sqrMagnitude;
				if (curDistance < distance) {
					closest = go;
					distance = curDistance;
				}
			}
			return closest;
		}
	}
}

