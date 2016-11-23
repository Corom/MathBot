#pragma once
#include "LiquidTWI2.h"
#include "ILcdDisplay.h"

namespace LcdHelper
{

	public ref class LcdDisplay sealed : public ILcdDisplay
	{
	public:
		LcdDisplay(int i2cAddr);

		virtual void SetBacklight(bool state);
		virtual void Enable(bool state);
		virtual void SetCursor(int x, int y);
		virtual void Clear();
		virtual void Print(Platform::String^ text);

	private:
		LiquidTWI2 lcd;
	};

}