
import React, { useState, useEffect, useRef } from 'react';
import * as THREE from 'three';
import * as CANNON from 'cannon-es';

interface DiceRoll {
  id: string;
  value: number;
  color: 'black' | 'yellow';
}

interface DiceRollerProps {
  onClose: () => void;
}

const DiceRoller: React.FC<DiceRollerProps> = ({ onClose }) => {
  const [blackCount, setBlackCount] = useState(1);
  const [yellowCount, setYellowCount] = useState(0);
  const [isRolling, setIsRolling] = useState(false);
  const [currentResults, setCurrentResults] = useState<DiceRoll[]>([]);
  const [history, setHistory] = useState<DiceRoll[][]>([]);
  
  const containerRef = useRef<HTMLDivElement>(null);
  const requestRef = useRef<number | undefined>(undefined);
  const worldRef = useRef<CANNON.World | undefined>(undefined);
  const sceneRef = useRef<THREE.Scene | undefined>(undefined);
  const cameraRef = useRef<THREE.PerspectiveCamera | undefined>(undefined);
  const rendererRef = useRef<THREE.WebGLRenderer | undefined>(undefined);
  const diceBodiesRef = useRef<{ body: CANNON.Body, mesh: THREE.Mesh, color: 'black' | 'yellow' }[]>([]);

  // Initialize Three.js and Cannon.js
  useEffect(() => {
    if (!containerRef.current) return;

    // --- Evitar duplicar renderer ---
    if (rendererRef.current) return;
    
    const container = containerRef.current;

    // --- PHYSICS SETUP ---
    const world = new CANNON.World();
    world.gravity.set(0, -9.82, 0);
    worldRef.current = world;

    // Floor physics
    const floorBody = new CANNON.Body({
      type: CANNON.Body.STATIC,
      shape: new CANNON.Plane(),
    });
    floorBody.quaternion.setFromEuler(-Math.PI / 2, 0, 0);
    world.addBody(floorBody);

    // Walls physics (to keep dice in view)
    const wallShape = new CANNON.Plane();
    const walls = [
      { pos: [0, 0, 5], rot: [0, Math.PI, 0] },    // Back
      { pos: [0, 0, -5], rot: [0, 0, 0] },       // Front
      { pos: [5, 0, 0], rot: [0, -Math.PI / 2, 0] }, // Right
      { pos: [-5, 0, 0], rot: [0, Math.PI / 2, 0] }   // Left
    ];
    walls.forEach(w => {
      const b = new CANNON.Body({ type: CANNON.Body.STATIC, shape: wallShape });
      b.position.set(w.pos[0], w.pos[1], w.pos[2]);
      b.quaternion.setFromEuler(w.rot[0], w.rot[1], w.rot[2]);
      world.addBody(b);
    });

    // --- THREE.JS SETUP ---
    // Start with default values, ResizeObserver will fix it immediately
    const scene = new THREE.Scene();
    sceneRef.current = scene;

    const camera = new THREE.PerspectiveCamera(45, 1, 0.1, 100);
    // Adjusted position: moved from (0, 10, 8) to (0, 14, 11) to be "a little far"
    camera.position.set(0, 14, 11);
    camera.lookAt(0, 0, 0);
    cameraRef.current = camera;

    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setPixelRatio(window.devicePixelRatio);
    // Limpiar cualquier canvas previo
    container.innerHTML = '';
    container.appendChild(renderer.domElement);
    //containerRef.current.appendChild(renderer.domElement);
    rendererRef.current = renderer;

    // Lighting
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.5);
    scene.add(ambientLight);
    const spotLight = new THREE.SpotLight(0x25f46a, 200);
    spotLight.position.set(5, 10, 5);
    spotLight.castShadow = true;
    scene.add(spotLight);

    // Floor Mesh
    const floorGeom = new THREE.PlaneGeometry(20, 20);
    const floorMat = new THREE.MeshStandardMaterial({ 
      color: 0x050a06,
      transparent: true,
      opacity: 0.8
    });
    const floorMesh = new THREE.Mesh(floorGeom, floorMat);
    floorMesh.rotation.x = -Math.PI / 2;
    scene.add(floorMesh);

    // Grid Helper for that retro look
    const grid = new THREE.GridHelper(20, 20, 0x25f46a, 0x1a853d);
    grid.position.y = 0.01;
    scene.add(grid);

    // Resize Handler
    const updateSize = () => {
      if (!containerRef.current || !cameraRef.current || !rendererRef.current) return;
      const width = containerRef.current.clientWidth;
      const height = containerRef.current.clientHeight;
      if (width === 0 || height === 0) return;

      cameraRef.current.aspect = width / height;
      cameraRef.current.updateProjectionMatrix();
      cameraRef.current.lookAt(0, 0, 0); // Re-center
      rendererRef.current.setSize(width, height);
    };

    // Use ResizeObserver for robust layout detection
    const resizeObserver = new ResizeObserver(() => {
      updateSize();
    });
    resizeObserver.observe(containerRef.current);

    // Animation Loop
    const animate = () => {
      world.fixedStep();
      
      // Update dice meshes
      diceBodiesRef.current.forEach(({ body, mesh }) => {
        mesh.position.copy(body.position as any);
        mesh.quaternion.copy(body.quaternion as any);
      });

      renderer.render(scene, camera);
      requestRef.current = requestAnimationFrame(animate);
    };
    animate();

    const handleResize = () => updateSize();
    window.addEventListener('resize', handleResize);

    return () => {
      window.removeEventListener('resize', handleResize);
      resizeObserver.disconnect();
      if (requestRef.current) cancelAnimationFrame(requestRef.current);
      
      // Eliminar todos los meshes de la escena
    diceBodiesRef.current.forEach(({ mesh }) => scene.remove(mesh));
    diceBodiesRef.current = [];
    if (rendererRef.current && container) {
        container.removeChild(rendererRef.current.domElement);
        rendererRef.current.dispose();
      rendererRef.current = undefined;
      }
      // Limpiar referencias
    sceneRef.current = undefined;
    cameraRef.current = undefined;
    worldRef.current = undefined;
    };
  }, []);

  const createDice = (color: 'black' | 'yellow') => {
    const scene = sceneRef.current;
    const world = worldRef.current;
    if (!scene || !world) return;

    // --- Cannon Body ---
    const shape = new CANNON.Box(new CANNON.Vec3(0.5, 0.5, 0.5));
    const body = new CANNON.Body({ mass: 1, shape });
    
    // Random position at top
    body.position.set(
      Math.random() * 4 - 2,
      5 + Math.random() * 2,
      Math.random() * 4 - 2
    );
    
    // Random rotation
    body.quaternion.setFromEuler(
      Math.random() * Math.PI,
      Math.random() * Math.PI,
      Math.random() * Math.PI
    );

    // Initial toss
    body.angularVelocity.set(
      Math.random() * 10,
      Math.random() * 10,
      Math.random() * 10
    );
    body.velocity.set(0, -2, 0);

    world.addBody(body);

    // --- Three Mesh ---
    const geometry = new THREE.BoxGeometry(1, 1, 1);
    
    // Create face materials with pips
    const materials = [1, 2, 3, 4, 5, 6].map(num => {
      const canvas = document.createElement('canvas');
      canvas.width = 128;
      canvas.height = 128;
      const ctx = canvas.getContext('2d')!;
      
      // Background
      ctx.fillStyle = color === 'black' ? '#050a06' : '#eab308';
      ctx.fillRect(0, 0, 128, 128);
      
      // Border
      ctx.strokeStyle = color === 'black' ? '#25f46a' : '#000000';
      ctx.lineWidth = 4;
      ctx.strokeRect(0, 0, 128, 128);

      // Pips
      ctx.fillStyle = color === 'black' ? '#25f46a' : '#000000';
      const drawPip = (x: number, y: number) => {
        ctx.beginPath();
        ctx.arc(x, y, 10, 0, Math.PI * 2);
        ctx.fill();
        if (color === 'black') {
          ctx.shadowBlur = 10;
          ctx.shadowColor = '#25f46a';
        }
      };

      const positions: Record<number, [number, number][]> = {
        1: [[64, 64]],
        2: [[32, 32], [96, 96]],
        3: [[32, 32], [64, 64], [96, 96]],
        4: [[32, 32], [32, 96], [96, 32], [96, 96]],
        5: [[32, 32], [32, 96], [64, 64], [96, 32], [96, 96]],
        6: [[32, 32], [32, 64], [32, 96], [96, 32], [96, 64], [96, 96]]
      };

      positions[num].forEach(([x, y]) => drawPip(x, y));

      const texture = new THREE.CanvasTexture(canvas);
      return new THREE.MeshStandardMaterial({ map: texture });
    });

    const mesh = new THREE.Mesh(geometry, materials);
    scene.add(mesh);

    return { body, mesh, color };
  };

  const getDiceValue = (body: CANNON.Body) => {
    const faces = [
      new CANNON.Vec3(1, 0, 0),  // 1
      new CANNON.Vec3(-1, 0, 0), // 2
      new CANNON.Vec3(0, 1, 0),  // 3
      new CANNON.Vec3(0, -1, 0), // 4
      new CANNON.Vec3(0, 0, 1),  // 5
      new CANNON.Vec3(0, 0, -1), // 6
    ];

    let maxUp = -Infinity;
    let value = 1;

    faces.forEach((faceNormal, index) => {
      const worldNormal = body.quaternion.vmult(faceNormal);
      if (worldNormal.y > maxUp) {
        maxUp = worldNormal.y;
        value = index + 1;
      }
    });

    return value;
  };

  const rollDice = async () => {
    if (isRolling) return;
    setIsRolling(true);
    setCurrentResults([]);

    // Clear old dice
    diceBodiesRef.current.forEach(({ body, mesh }) => {
      worldRef.current?.removeBody(body);
      sceneRef.current?.remove(mesh);
    });
    diceBodiesRef.current = [];

    // Create new dice
    const newDice = [];
    for (let i = 0; i < blackCount; i++) newDice.push(createDice('black'));
    for (let i = 0; i < yellowCount; i++) newDice.push(createDice('yellow'));
    
    diceBodiesRef.current = newDice.filter(d => !!d) as any;

    let attempts = 0;
    const checkSettled = setInterval(() => {
      const allSettled = diceBodiesRef.current.every(d => 
        d.body.velocity.length() < 0.1 && d.body.angularVelocity.length() < 0.1
      );
      
      attempts++;
      if (allSettled || attempts > 100) {
        clearInterval(checkSettled);
        const results: DiceRoll[] = diceBodiesRef.current.map(d => ({
          id: Math.random().toString(36).substr(2, 9),
          value: getDiceValue(d.body),
          color: d.color
        }));
        setCurrentResults(results);
        setHistory(prev => [results, ...prev].slice(0, 10));
        setIsRolling(false);
      }
    }, 100);
  };

  const total = currentResults.reduce((acc, curr) => acc + curr.value, 0);
  const successes = currentResults.filter(r => r.value === 6).length;
  const fails = currentResults.filter(r => r.value === 1).length;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/90 backdrop-blur-md p-4 font-mono">
      <div className="w-full max-w-6xl h-[85vh] bg-surface-dark border-2 border-primary/40 rounded-lg overflow-hidden flex flex-col shadow-[0_0_80px_rgba(37,244,106,0.15)] relative">
        {/* Header */}
        <div className="bg-primary/5 border-b border-primary/20 p-4 flex justify-between items-center z-10">
          <div className="flex items-center gap-3">
            <span className="material-icons text-primary animate-pulse">view_in_ar</span>
            <div className="flex flex-col">
              <span className="text-sm font-bold tracking-widest text-primary uppercase">Módulo de Realidad Virtual Dice_Gen</span>
              <span className="text-[10px] text-primary/40 uppercase">Simulación Física: Precision H-99</span>
            </div>
          </div>
          <button onClick={onClose} className="text-primary/60 hover:text-primary transition-colors hover:rotate-90">
            <span className="material-icons">close</span>
          </button>
        </div>

        <div className="flex-1 flex overflow-hidden">
          {/* Left Panel: Stats & Controls */}
          <aside className="w-80 border-r border-primary/20 flex flex-col bg-black/40 z-10">
            {/* Controls */}
            <div className="p-6 space-y-6 border-b border-primary/10">
              <h3 className="text-[10px] text-primary/60 uppercase tracking-widest font-bold">// Configuración</h3>
              <div className="space-y-4">
                <div className="flex justify-between items-center bg-black/30 p-2 border border-primary/10 clip-tech-tl">
                  <label className="text-[10px] text-primary/70 uppercase">Negros</label>
                  <div className="flex items-center gap-4">
                    <button onClick={() => setBlackCount(Math.max(0, blackCount - 1))} className="text-primary hover:text-white transition-colors">
                      <span className="material-icons text-sm">remove</span>
                    </button>
                    <span className="w-8 text-center text-primary font-bold">{blackCount}</span>
                    <button onClick={() => setBlackCount(Math.min(10, blackCount + 1))} className="text-primary hover:text-white transition-colors">
                      <span className="material-icons text-sm">add</span>
                    </button>
                  </div>
                </div>
                <div className="flex justify-between items-center bg-black/30 p-2 border border-yellow-500/10 clip-tech-tl">
                  <label className="text-[10px] text-yellow-500/70 uppercase">Amarillos</label>
                  <div className="flex items-center gap-4">
                    <button onClick={() => setYellowCount(Math.max(0, yellowCount - 1))} className="text-yellow-500 hover:text-white transition-colors">
                      <span className="material-icons text-sm">remove</span>
                    </button>
                    <span className="w-8 text-center text-yellow-500 font-bold">{yellowCount}</span>
                    <button onClick={() => setYellowCount(Math.min(10, yellowCount + 1))} className="text-yellow-500 hover:text-white transition-colors">
                      <span className="material-icons text-sm">add</span>
                    </button>
                  </div>
                </div>
              </div>
              <button 
                onClick={rollDice}
                disabled={isRolling || (blackCount + yellowCount === 0)}
                className="w-full h-14 bg-primary text-black font-bold uppercase tracking-widest hover:scale-[1.02] active:scale-95 transition-all shadow-[0_0_20px_rgba(37,244,106,0.2)] disabled:opacity-20 flex items-center justify-center gap-2 overflow-hidden px-4"
              >
                {isRolling ? (
                  <span className="material-icons animate-spin text-lg">refresh</span>
                ) : (
                  <span className="material-icons text-lg">casino</span>
                )}
                <span className="text-sm tracking-[0.2em] whitespace-nowrap">LANZAR</span>
              </button>
            </div>

            {/* Current Result Display */}
            <div className="flex-1 flex flex-col p-6 overflow-hidden">
               <h3 className="text-[10px] text-primary/60 uppercase tracking-widest font-bold mb-4">// Historial_Sesión</h3>
               
               <div className="flex-1 overflow-y-auto custom-scrollbar space-y-4">
                  {currentResults.length > 0 && (
                    <div className="bg-primary/5 border-l-2 border-primary p-3 animate-glitch-in">
                      <p className="text-[8px] text-primary/40 uppercase mb-2">Última Tirada</p>
                      
                      <div className="grid grid-cols-2 gap-2 mb-4">
                        <div className="bg-black/40 border border-primary/20 p-2 text-center">
                          <p className="text-[7px] text-primary/60 uppercase mb-1">Éxitos [6]</p>
                          <p className="text-3xl font-display font-bold text-primary text-glow leading-none">{successes}</p>
                        </div>
                        <div className="bg-black/40 border border-danger/20 p-2 text-center">
                          <p className="text-[7px] text-danger/60 uppercase mb-1">Fallos [1]</p>
                          <p className="text-3xl font-display font-bold text-danger leading-none">{fails}</p>
                        </div>
                      </div>

                      <div className="flex flex-wrap gap-1.5 mb-3">
                        {currentResults.map(r => (
                          <span key={r.id} className={`w-6 h-6 flex items-center justify-center text-[10px] font-bold border transition-all ${
                            r.value === 6 ? 'border-primary ring-1 ring-primary/40' : 
                            r.value === 1 ? 'border-danger' : 'border-primary/20'
                          } ${
                            r.color === 'black' ? 'bg-black text-primary' : 'bg-yellow-500 text-black'
                          }`}>
                            {r.value}
                          </span>
                        ))}
                      </div>

                      <div className="flex justify-between items-center border-t border-primary/10 pt-2">
                         <span className="text-[8px] text-primary/40 uppercase tracking-widest">Suma Bruta:</span>
                         <span className="text-lg font-display font-bold text-primary/80 leading-none">{total}</span>
                      </div>
                    </div>
                  )}

                  {history.slice(1).map((h, i) => {
                    const hSucc = h.filter(r => r.value === 6).length;
                    const hFail = h.filter(r => r.value === 1).length;
                    const hTotal = h.reduce((a, b) => a + b.value, 0);
                    return (
                      <div key={i} className="opacity-40 hover:opacity-100 transition-opacity border-l border-primary/10 pl-3">
                        <div className="flex justify-between items-center mb-1">
                          <p className="text-[8px] uppercase">Registro #{history.length - i - 1}</p>
                          <div className="flex gap-2">
                            <span className="text-[8px] text-primary font-bold">É:{hSucc}</span>
                            <span className="text-[8px] text-danger font-bold">F:{hFail}</span>
                          </div>
                        </div>
                        <div className="flex gap-1 text-[8px] flex-wrap">
                          {h.map(r => (
                            <span key={r.id} className={`${r.value === 6 ? 'text-primary font-bold' : r.value === 1 ? 'text-danger font-bold' : 'text-primary/40'}`}>
                              {r.value}
                            </span>
                          ))}
                          <span className="ml-auto text-[7px] text-primary/30 uppercase">Σ {hTotal}</span>
                        </div>
                      </div>
                    );
                  })}

                  {history.length === 0 && !isRolling && (
                    <div className="h-full flex flex-col items-center justify-center text-primary/20 text-center gap-2">
                      <span className="material-icons text-4xl">inventory_2</span>
                      <p className="text-[10px] uppercase">Esperando secuencia<br/>de lanzamiento</p>
                    </div>
                  )}
               </div>
            </div>
          </aside>

          {/* Right Panel: 3D Arena */}
          <main className="flex-1 relative bg-black/20 cursor-move">
            <div ref={containerRef} className="w-full h-full" />
            
            {/* Arena Overlays */}
            <div className="absolute top-4 right-4 pointer-events-none text-right">
               <p className="text-[8px] text-primary/40 uppercase">Render: Real_Time // FPS: 60</p>
               <p className="text-[8px] text-primary/40 uppercase">Coordenadas: 0,0,0</p>
            </div>

            <div className="absolute bottom-4 left-4 pointer-events-none">
              <div className="flex gap-1">
                {[...Array(5)].map((_, i) => (
                  <div key={i} className="w-1 h-4 bg-primary/20" />
                ))}
              </div>
              <p className="text-[8px] text-primary/40 uppercase mt-1">Status: Operational</p>
            </div>

            {isRolling && (
              <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                <div className="flex flex-col items-center gap-4">
                  <div className="w-24 h-1 bg-primary/10 relative overflow-hidden">
                     <div className="absolute inset-0 bg-primary animate-[scan_1s_linear_infinite]" />
                  </div>
                  <span className="text-primary text-[10px] font-bold uppercase tracking-[0.3em] animate-pulse">Calculando Probabilidades</span>
                </div>
              </div>
            )}
          </main>
        </div>

        {/* Footer */}
        <div className="bg-black p-2 border-t border-primary/20 flex justify-between items-center text-[8px] text-primary/30 uppercase tracking-widest">
           <span>Engine: Three.js r160</span>
           <span>World: Cannon-es PhysX</span>
           <span>Auth: Secure_Session_0x1</span>
        </div>
      </div>
    </div>
  );
};

export default DiceRoller;
