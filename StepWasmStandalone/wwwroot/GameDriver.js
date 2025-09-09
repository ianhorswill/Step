let gameObjects = {};
let gameObjectArray = [];

///
/// Initialization
///

async function startGame(animatedSprites, staticSprites) {
    console.log("startGame called");
    try {
        if (typeof game === 'undefined' || game === null) {
            game = new PIXI.Application();
            await game.init({ background: '#363' });

            const page = document.getElementById('codeOutput').parentElement;
            window.requestAnimationFrame(function () { page.appendChild(game.canvas); console.log("canvas added") });
            //await addAnimatedSprite('bunny', 'spritesheets/steampunk_m10.png', 500, 500, false, false, false);
            //gotoXY('bunny', 50, 50, 3);
            game.ticker.add(updateGameObjects);
            console.log("started ticker");
        }
        else {
            destroyAllGameObjects();
        }

        var characters = Promise.all(animatedSprites.map(
            args => addAnimatedSprite(args[0], args[1], args[2], args[3], args[4], args[5], args[6])));
        var other = Promise.all(staticSprites.map(
            args => addStaticSprite(args[0], args[1], args[2], args[3], args[4], args[5], args[6])));

        await characters;
        await other;
    } catch (e) {
        console.log('error in startGame');
        console.log(e);
        return false;
    }
    postNotifications();
    return true;
}

///
/// Object creation and destruction
///

async function addStaticSprite(name, textureFile, x, y, destroyOffScreen, explosive, immovable) {
    try {
        if (!textureFile.endsWith('.png') && !textureFile.startsWith('http'))
            textureFile = 'sprites/' + textureFile + '.png';

        const container = new PIXI.Container();
        container.x = x;
        container.y = y;
        game.stage.addChild(container);
        const o = {
            name: name,
            container: container,
            destroyWhenOffScreen: destroyOffScreen,
            explosive: explosive,
            immovable: immovable,
            onChangeVelocity: nop,
            stopAt: null
        };
        stop(o);

        const text = new PIXI.HTMLText({ text: name, style: { align: 'center' } });
        o.text = text;
        text.y = -40;
        container.addChild(text);

        gameObjects[name] = o;
        gameObjectArray = Object.values(gameObjects);
        const texture = await PIXI.Assets.load(textureFile);
        o.bboxObject = new PIXI.Sprite(texture);
        container.addChild(o.bboxObject);
        console.log("added "+name)
    } catch (e) {
        console.log('error in addStaticSprite');
        console.log(e);
        return false;
    }
    return true;
}

async function addAnimatedSprite(name, textureFile, x, y, destroyOffScreen, explosive, immovable) {
    try {
        if (!textureFile.endsWith('.png') && !textureFile.startsWith('http'))
            textureFile = 'spritesheets/' + textureFile + '.png';
        const container = new PIXI.Container();
        container.x = x;
        container.y = y;
        game.stage.addChild(container);

        const w = 32;
        const h = 52;
        const atlasData = {
            frames: {
                south1: {
                    frame: { x: 0, y: 0, w: w, h: h },
                },
                south2: {
                    frame: { x: 32, y: 0, w: w, h: h },
                },
                south3: {
                    frame: { x: 64, y: 0, w: w, h: h },
                },
                south4: {
                    frame: { x: 96, y: 0, w: w, h: h },
                },
                west1: {
                    frame: { x: 0, y: h, w: w, h: h },
                },
                west2: {
                    frame: { x: 32, y: h, w: w, h: h },
                },
                west3: {
                    frame: { x: 64, y: h, w: w, h: h },
                },
                west4: {
                    frame: { x: 96, y: h, w: w, h: h },
                },
                east1: {
                    frame: { x: 0, y: 2*h, w: w, h: h },
                },
                east2: {
                    frame: { x: 32, y: 2*h, w: w, h: h },
                },
                east3: {
                    frame: { x: 64, y: 2*h, w: w, h: h },
                },
                east4: {
                    frame: { x: 96, y: 2*h, w: w, h: h },
                },
                north1: {
                    frame: { x: 0, y: 3 * h, w: w, h: h },
                },
                north2: {
                    frame: { x: 32, y: 3 * h, w: w, h: h },
                },
                north3: {
                    frame: { x: 64, y: 3 * h, w: w, h: h },
                },
                north4: {
                    frame: { x: 96, y: 3 * h, w: w, h: h },
                },
            },
            meta: {
                image: textureFile,
                format: 'RGBA8888',
                size: { w: 128, h: 208 },
                scale: 1,
            },
            animations: {
                south: ['south1', 'south2', 'south3', 'south4'], 
                west: ['west1', 'west2', 'west3', 'west4'], 
                east: ['east1', 'east2', 'east3', 'east4'], 
                north: ['north1', 'north2', 'north3', 'north4'], 
            },
        };

        const texture = await PIXI.Assets.load(textureFile);
        const spritesheet = new PIXI.Spritesheet(texture, atlasData);

        // Generate all the Textures asynchronously
        await spritesheet.parse();
        // spritesheet is ready to use!
        const anim = new PIXI.AnimatedSprite(spritesheet.animations.south);
        anim.textures = spritesheet.animations.north;

        // set the animation speed
        anim.animationSpeed = 0.1666;
        container.addChild(anim);

        const o = {
            name: name,
            container: container,
            bboxObject: anim,
            destroyWhenOffScreen: destroyOffScreen,
            explosive: explosive,
            immovable: immovable,
            onChangeVelocity: function () {
                const x = o.xVelocity;
                const y = o.yVelocity;
                if (x === 0 && y === 0) {
                    if (o.animationState != 'stop') {
                        anim.textures = spritesheet.animations.south;
                        anim.gotoAndStop(0);
                        o.animationState = 'stop'
                    }
                } else if (Math.abs(x) > Math.abs(y)) {
                    if (x > 0) {
                        if (o.animationState != 'east') {
                            anim.textures = spritesheet.animations.east;
                            anim.gotoAndPlay(0);
                            o.animationState = 'east';
                        }
                    } else {
                        if (o.animationState != 'west') {
                            anim.textures = spritesheet.animations.west;
                            anim.gotoAndPlay(0);
                            o.animationState = 'west';
                        }
                    }
                } else {
                    if (y < 0) {
                        if (o.animationState != 'north') {
                            anim.textures = spritesheet.animations.north;
                            anim.gotoAndPlay(0);
                            o.animationState = 'north';
                        }
                    } else {
                        if (o.animationState != 'south') {
                            anim.textures = spritesheet.animations.south;
                            anim.gotoAndPlay(0);
                            o.animationState = 'south';
                        }
                    }
                }
            },
            stopAt: null
        };
        stop(o);

        const text = new PIXI.HTMLText({ text: name, style: { align: 'center' } });
        o.text = text;
        text.y = -40;
        container.addChild(text);

        gameObjects[name] = o;
        gameObjectArray = Object.values(gameObjects);
        console.log("added " + name)
    } catch (e) {
        console.log('error in addAnimatedSprite');
        console.log(e);
        return false;
    }
    notify('spawn', name);
    return true;
}

function destroyGameObject(o) {
    if (o.immovable) return;  // it's a wall or something
    game.stage.removeChild(o.container);
    delete gameObjects[o.name];
    gameObjectArray = Object.values(gameObjects);
}

function destroyAllGameObjects() {
    while (game.stage.children[0])
        game.stage.removeChild(game.stage.children[0]);
    gameObjects = {}
    gameObjectArray = [];
}

///
/// Physics, such as it is
///

let notifications = [];
function updateGameObjects(time) {
    try {
        const w = game.screen.right;
        const h = game.screen.bottom;

        notifications = [];

        for (let i = 0; i < gameObjectArray.length; i++) {
            const o = gameObjectArray[i];
            const c = o.container;
            o.oldX = c.x;
            o.oldY = c.y;
            o.controller();
            c.x += o.xVelocity * time.deltaTime;
            c.y += o.yVelocity * time.deltaTime;
            const b = o.bboxObject.getBounds();
            if (b.minX < 0 || b.minY < 0 || b.maxX > w || b.maxY > h) {
                if (o.destroyWhenOffScreen)
                    destroyGameObject(o);
                else {
                    // we moved off screen; undo the movement
                    c.x = o.oldX;
                    c.y = o.oldY;
                }
            }
        }

        let toDestroy = [];
        for (let i = 0; i < gameObjectArray.length; i++) {
            const io = gameObjectArray[i];
            const ib = io.bboxObject.getBounds();
            for (let j = i + 1; j < gameObjectArray.length; j++) {
                const jo = gameObjectArray[j];
                const jb = jo.bboxObject.getBounds();
                if (overlap(ib, jb))
                {
                    if (io.explosive || jo.explosive) {
                        if (toDestroy.indexOf(io) < 0)
                            toDestroy.push(io);
                        if (toDestroy.indexOf(jo) < 0)
                            toDestroy.push(jo);
                    }

                    if (io.stopAt == jo || jo.immovable) {
                        stop(io);
                        notifyArrived(io, jo);
                    }

                    if (jo.stopAt == io || io.immovable) {
                        stop(jo);
                        notifyArrived(jo, io);
                    }
                }
            }
        }

        // Destroy anything we need to destroy
        for (let i = 0; i < toDestroy.length; i++)
            destroyGameObject(toDestroy[i]);

        if (notifications.length > 0)
            postNotifications();
    } catch (e) {
        console.log("error in addStaticSprite");
        console.log(e);
    }
}

function overlap(b1, b2) {
    return !(b1.maxX < b2.minX || b2.maxX < b1.minX || b1.maxY < b2.minY || b2.maxY < b1.minY)
}

function notifyArrived(o, destination) {
    if (destination != null)
        destination = destination.name;
    notify('arrived', o.name, destination);
}

function notify(...notification) {
    notifications.push(notification);
}

function postNotifications() {
    if (notifications.length > 0) {
        DotNet.invokeMethodAsync('StepWasmStandalone', 'ProcessGameNotifications',
            notifications,
            gameObjectArray.map(o => [o.name, o.container.x, o.container.y])
        );
    }
    notifications = [];
}

///
/// Movement controllers
///

function setConstantVelocity(gameObject, vx, vy) {
    if (typeof gameObject == 'string')
        gameObject = gameObjects[gameObject];
    gameObject.xVelocity = vx;
    gameObject.yVelocity = vy;
    gameObject.onChangeVelocity();
    gameObject.controller = nop;
}

function stop(gameObject) {
    console.log(gameObject.name + " stop");
    setConstantVelocity(gameObject, 0, 0);
    gameObject.stopAt = null;
}

function gotoXY(agent, x, y, speed) {
    if (typeof agent == 'string')
        agent = gameObjects[agent];
    console.log(agent.name + "->" + x + ',' + y + ' ' + speed);

    agent.controller = function () {
        const dx = x - agent.container.x;
        const dy = y - agent.container.y;
        const distance = Math.sqrt(dx * dx + dy * dy);
        if (distance < speed) {
            stop(agent);
            // Teleport to the final location
            agent.container.x = x;
            agent.container.y = y;
            notifyArrived(agent, null);
        } else {
            const k = speed / Math.max(1, distance);
            agent.xVelocity = k * dx;
            agent.yVelocity = k * dy;
            agent.onChangeVelocity();
        }
    }
}

function gotoGameObject(agent, dest, speed) {
    if (typeof agent == 'string')
        agent = gameObjects[agent];

    if (typeof dest == 'string')
        dest = gameObjects[dest];

    agent.stopAt = dest;

    agent.controller = function () {
        const dx = dest.container.x - agent.container.x;
        const dy = dest.container.y - agent.container.y;
        const distance = Math.sqrt(dx * dx + dy * dy);
        const k = speed / Math.max(1, distance);
        agent.xVelocity = k * dx;
        agent.yVelocity = k * dy;
        agent.onChangeVelocity();
    }
}

// Used as the controller for static velocities
function nop() { }

///
/// Other actions
///

function setText(gameObject, text) {
    if (typeof gameObject == 'string')
        gameObject = gameObjects[gameObject];
    gameObject.text.text = text;
}