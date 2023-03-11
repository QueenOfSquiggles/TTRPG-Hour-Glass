extends Button

@export var icon_open : Texture
@export var icon_closed : Texture

func _ready():
	_on_toggled(button_pressed)

func _on_toggled(button_pressed):
	icon = icon_open if button_pressed\
			else icon_closed
