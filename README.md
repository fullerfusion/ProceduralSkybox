<img width="1573" alt="Screenshot 2025-06-05 at 5 53 08 AM" src="https://github.com/user-attachments/assets/71af71ac-26f6-4641-8ea6-11646ab76df9" />

# Procedural Skybox
This is a procedural skybox shader using Unity's shadergraph for URP. Currently, it's still a WIP, but the goal is have support for Day & Night Cycle, Clouds, Stars. 

## Requirements
- Unity 6000.1.4f1
- URP - 17.1.0
- Shadergraph - 17.1.0

## Setup
The skybox shadergraph, sub shadergraphs, & materials can be found in the Shaders folder (Assets>Shaders). 
<img width="359" alt="Screenshot 2025-06-05 at 6 13 44 AM" src="https://github.com/user-attachments/assets/13b96efd-c3a9-474f-958a-4d6b53177c0b" />

For player and camera movement, I'm using Unity's starter asset for Third-Person controls. it uses the standard WASD for locomotion movement and the mouse for camera rotation. Please do not alter or change any C# scripts or update Cinemachine to version 3.0+ as that will break the demo C# script provided!



## Features
The skybox material has multiple properities enabled in the Inspector for ease of access and quick changes. It features the four main sky states: Sunrise, Daytime, Sunset, & Nighttime. These are made up of three HDR colors; Zenith, Mid, & Horizon color.

<img width="603" alt="Screenshot 2025-06-05 at 5 59 37 AM" src="https://github.com/user-attachments/assets/74c90ab4-b707-4e0e-8043-baac513a78ee" />

The main shadergraph is organized with redirect nodes & group selection for ease of access. ~~I problably spend too much time making sure the spiderweb of paths are aligned and straight~~. The current issue I'm having is 

<img width="1300" alt="Screenshot 2025-06-05 at 6 17 38 AM" src="https://github.com/user-attachments/assets/62192f6b-5ca7-4a84-88dd-8052217aaff6" />

## Contributing

Pull requests are welcome! For major changes, please open an issue first
to discuss what you would like to change.

## License

[MIT](https://choosealicense.com/licenses/mit/)
