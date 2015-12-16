﻿using System;
using System.Collections.Generic;

namespace Main
{
	[VDFType(propIncludeRegexL1: "", popOutL1: true)] public class Day
	{
		[VDFPreDeserialize] protected Day() {} // makes-so in-code defaults are used, for props that aren't set in the VDF
		public Day(DateTime date) { this.date = date; }

		public DateTime date; // utc; convenience property (you could, instead, just get it from the filename)
		[VDFProp(popOutL2: true)] public List<Session> sessions = new List<Session>();
	}
}