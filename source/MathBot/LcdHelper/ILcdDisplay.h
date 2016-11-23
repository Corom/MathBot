#pragma once


namespace LcdHelper
{

	public interface class ILcdDisplay
	{
	public:
		void SetBacklight(bool state);
		void Enable(bool state);
		void SetCursor(int x, int y);
		void Clear();
		void Print(Platform::String^ text);
	};

}