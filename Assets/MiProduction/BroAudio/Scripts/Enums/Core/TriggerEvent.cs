using System;

[Flags]
public enum TriggerEvent
{
	OnTriggerEnter = 1,
	OnTriggerEnter2D = 2,
	OnTriggerExit = 4,
	OnTriggerExit2D = 8,
}