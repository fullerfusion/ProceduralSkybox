<img width="1653" alt="Screenshot 2025-06-05 at 5 53 08 AM" src="https://github.com/user-attachments/assets/41a51dc2-2304-48b7-873f-16c6066f14ca" />

# Procedural Skybox
This is a procedural skybox shader using Unity's shadergraph I've been working on that will have support for Day & Night Cycle. Currently, it's still a WIP.

## Requirements
- Unity 6000.1.4f1
- URP - 17.1.0
- Shadergraph - 17.1.0

## Setup
The skyboy shadergraph, subshadergraphs, & materials can be found in the Shaders folder (Assets>Shaders). 
For player and caemra movement, I'm using Unity's starter asset for Third-Person controls. Please do not alter or change any C# scripts or update Cinemachine to version 3.0+ as that will break the demo C# script provided. 


## Features
The skybox material has multiple properities enabled in the Inspector for ease of access and quick changes. It features the four main sky states: Sunrise, Daytime, Sunset, & Nighttime. These are made up of three HDR colors; Zenith, Mid, & Horizon color.

<img width="603" alt="Screenshot 2025-06-05 at 5 59 37 AM" src="https://github.com/user-attachments/assets/74c90ab4-b707-4e0e-8043-baac513a78ee" />


## Contributing

Pull requests are welcome! For major changes, please open an issue first
to discuss what you would like to change.

## License

[MIT](https://choosealicense.com/licenses/mit/)
