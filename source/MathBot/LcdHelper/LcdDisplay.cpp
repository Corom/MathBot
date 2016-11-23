#include "LcdDisplay.h"

using namespace LcdHelper;

LcdDisplay::LcdDisplay(int i2cPort) : lcd(i2cPort)
{
	lcd.setMCPType(LTI_TYPE_MCP23008);
	lcd.begin(16, 2);
}

void LcdDisplay::SetBacklight(bool state) {
	lcd.setBacklight(state ? HIGH : LOW);
}

void LcdDisplay::Enable(bool state) {
	if (state)
		lcd.display();
	else
		lcd.noDisplay();
}

void LcdDisplay::SetCursor(int x, int y)
{
	lcd.setCursor(x, y);
}
void LcdDisplay::Clear()
{
	lcd.clear();
}


void LcdDisplay::Print(Platform::String^ txt)
{
	// convert the string to a cstr
	std::wstring wstr(txt->Begin());
	std::string str(wstr.begin(), wstr.end());
	lcd.print(str.c_str());
}