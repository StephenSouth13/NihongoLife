Hello

If you have problems with some areas like skirts, you can always render both faces, disabling backface culling

FOR FBX USERS : 
Metallic, Roughness, Alpha may NOT WORK properly, 
you may be need to re plug them or ADJUST the value.


FOR UNITY USERS : 
If material have alpha : you'll need to enable alpha cliping in the material inspector's tab if there's alpha texture 
or if old rendering pipeline : change opaque to something :)
and you guys are also FBX USERS so you'll need to replug the materials if needed

FOR GLTF GLB USERS: 
nothing to do, should work fine :) maybe adjust roughness and metallic value when there's texture map for them


UNREAL ENGINE :
If you are using UNREAL ENGINE please USE GLB,GLTF so you don't have to setup all the materials, 
but if you persist to use fbx files, then when importing, CHECK FLIPPED -Y FOR NORMAL MAP option.
If you want to import the model without having all the parts separated, just check the option when importing


If you think my work is usefull, you can donate me a little thing in itch,
that would help me to make more FREE assets

 
https://styloo.itch.io/

:)

if you have any questions 
https://discord.gg/4PENNKE6yN