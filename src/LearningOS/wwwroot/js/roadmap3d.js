// LearningOS 3D Interactive Curriculum Roadmap (Powered by Three.js)
// Supports 4 Distinct 3D Architectural Visualizations:
// 1. Cosmic Helix (Ascending orbital momentum spiral)
// 2. 3D Neural Knowledge Graph (Clustered domain galaxy with cross-topic dependency synapses)
// 3. Cyberpunk Subway Grid (Multi-track elevated tech runway with holographic grid)
// 4. 3D Skill Tree Pyramid (Tiered ziggurat ascension summit)

let scene3D, camera3D, renderer3D, controls3D;
let nodesGroup, linesGroup, particlesGroup, labelsGroup;
let dependencyLinesGroup, cyberGridGroup, pyramidRingsGroup;
let dayNodesMap = new Map(); // dayNumber -> { mesh, data, initialScale, position, targetPosition, helixPos, graphPos, cyberPos, pyramidPos }
let dayDataList = [];
let hoveredNode = null;
let selectedNode = null;
let is3DInitialized = false;
let animationFrameId = null;
let raycaster, mouse;
let isAutoRotating = true;
let pulseProgress = 0;
let current3DMode = 'helix'; // 'helix' | 'graph' | 'cyber' | 'pyramid'
let isMorphing = false;
let morphProgress = 1.0;
let targetCameraPos = null;
let targetLookAt = null;

// Color Palette for 3D Cosmos
const PHASE_COLORS = {
  1: 0x38bdf8, // Phase 1: Advanced C# (.NET Cyan)
  2: 0x10b981, // Phase 2: Microservices (Emerald)
  3: 0xa855f7, // Phase 3: Applied AI/LLM (Electric Purple)
  4: 0x6366f1, // Phase 4: Portfolio Projects (Indigo)
  5: 0xf59e0b  // Phase 5: System Design Mastery (Gold/Amber)
};

const STATUS_COLORS = {
  Completed: 0x10b981,
  InProgress: 0xf59e0b,
  PartiallyCompleted: 0x8b5cf6,
  Missed: 0xef4444,
  Skipped: 0x64748b,
  RestDay: 0x14b8a6,
  LeaveDay: 0x0ea5e9,
  Planned: 0x38bdf8
};

// Entry point called when 3D Roadmap tab is activated
async function loadRoadmap3D() {
  const container = document.getElementById('roadmap-3d-canvas-container');
  if (!container) return;

  if (typeof THREE === 'undefined') {
    container.innerHTML = `
      <div class="flex flex-col items-center justify-center h-full text-slate-400 p-8 text-center">
        <div class="text-4xl mb-3">⚠️</div>
        <div class="text-base font-bold text-white mb-1">Three.js Engine Not Loaded</div>
        <p class="text-xs max-w-md">Please ensure Three.js is available to render the 3D cosmic roadmap.</p>
      </div>
    `;
    return;
  }

  // Fetch or use cached day data
  try {
    if (!allDaysCache || allDaysCache.length === 0) {
      allDaysCache = await apiCall('/api/days');
    }
    dayDataList = allDaysCache.sort((a, b) => (a.dayNumber || 0) - (b.dayNumber || 0));
  } catch (err) {
    console.error("Failed to load days for 3D roadmap", err);
  }

  if (!is3DInitialized) {
    init3DScene(container);
  } else {
    update3DNodes();
    on3DResize();
  }

  update3DHUDStats();
}

function init3DScene(container) {
  // Clear any existing content
  container.innerHTML = '';

  const width = container.clientWidth || 800;
  const height = container.clientHeight || 680;

  // 1. Scene & Background
  scene3D = new THREE.Scene();
  scene3D.fog = new THREE.FogExp2(0x030712, 0.0032);

  // 2. Camera
  camera3D = new THREE.PerspectiveCamera(55, width / height, 0.1, 2000);
  camera3D.position.set(0, 30, 160);

  // 3. Renderer
  renderer3D = new THREE.WebGLRenderer({ antialias: true, alpha: true });
  renderer3D.setSize(width, height);
  renderer3D.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer3D.toneMapping = THREE.ACESFilmicToneMapping;
  renderer3D.toneMappingExposure = 1.15;
  container.appendChild(renderer3D.domElement);

  // 4. OrbitControls
  if (typeof THREE.OrbitControls !== 'undefined') {
    controls3D = new THREE.OrbitControls(camera3D, renderer3D.domElement);
    controls3D.enableDamping = true;
    controls3D.dampingFactor = 0.05;
    controls3D.maxDistance = 450;
    controls3D.minDistance = 15;
    controls3D.autoRotate = isAutoRotating;
    controls3D.autoRotateSpeed = 0.6;
    controls3D.target.set(0, 5, 0);
  }

  // 5. Lighting
  const ambientLight = new THREE.AmbientLight(0xffffff, 0.75);
  scene3D.add(ambientLight);

  const keyLight = new THREE.DirectionalLight(0xffffff, 1.2);
  keyLight.position.set(100, 150, 100);
  scene3D.add(keyLight);

  const fillLight = new THREE.DirectionalLight(0x38bdf8, 0.8);
  fillLight.position.set(-100, -50, -100);
  scene3D.add(fillLight);

  const purplePoint = new THREE.PointLight(0xa855f7, 2.5, 300);
  purplePoint.position.set(0, 0, 0);
  scene3D.add(purplePoint);

  // 6. Starfield Background
  createStarfield();

  // 7. Groups
  nodesGroup = new THREE.Group();
  linesGroup = new THREE.Group();
  particlesGroup = new THREE.Group();
  labelsGroup = new THREE.Group();
  dependencyLinesGroup = new THREE.Group();
  cyberGridGroup = new THREE.Group();
  pyramidRingsGroup = new THREE.Group();

  scene3D.add(linesGroup);
  scene3D.add(particlesGroup);
  scene3D.add(dependencyLinesGroup);
  scene3D.add(cyberGridGroup);
  scene3D.add(pyramidRingsGroup);
  scene3D.add(nodesGroup);
  scene3D.add(labelsGroup);

  // 8. Build Sceneries for all modes
  buildCyberGridScenery();
  buildPyramidRingsScenery();

  // 9. Build 3D Curriculum Track & Nodes
  buildAllNodePositions();

  // 10. Raycasting Setup
  raycaster = new THREE.Raycaster();
  mouse = new THREE.Vector2(-999, -999);

  renderer3D.domElement.addEventListener('mousemove', on3DMouseMove);
  renderer3D.domElement.addEventListener('click', on3DMouseClick);
  renderer3D.domElement.addEventListener('touchstart', on3DTouchStart, { passive: true });

  window.addEventListener('resize', on3DResize);

  is3DInitialized = true;
  switch3DLayout('helix', true);
  animate3D();
}

// Generates an interstellar starfield with 1,200 twinkling stars
function createStarfield() {
  const starsCount = 1200;
  const geometry = new THREE.BufferGeometry();
  const positions = new Float32Array(starsCount * 3);
  const colors = new Float32Array(starsCount * 3);

  for (let i = 0; i < starsCount; i++) {
    const r = 300 + Math.random() * 600;
    const theta = Math.random() * Math.PI * 2;
    const phi = Math.acos(Math.random() * 2 - 1);

    positions[i * 3] = r * Math.sin(phi) * Math.cos(theta);
    positions[i * 3 + 1] = r * Math.sin(phi) * Math.sin(theta);
    positions[i * 3 + 2] = r * Math.cos(phi);

    // Subtle star colors (blue-white, gold, purple)
    const tint = Math.random();
    if (tint > 0.8) {
      colors[i * 3] = 0.65; colors[i * 3 + 1] = 0.85; colors[i * 3 + 2] = 1.0;
    } else if (tint > 0.6) {
      colors[i * 3] = 0.95; colors[i * 3 + 1] = 0.85; colors[i * 3 + 2] = 0.5;
    } else {
      colors[i * 3] = 0.8; colors[i * 3 + 1] = 0.8; colors[i * 3 + 2] = 0.9;
    }
  }

  geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
  geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));

  const material = new THREE.PointsMaterial({
    size: 2.2,
    vertexColors: true,
    transparent: true,
    opacity: 0.85
  });

  const starfield = new THREE.Points(geometry, material);
  scene3D.add(starfield);
}

// Build Scenery for Cyber Grid Mode
function buildCyberGridScenery() {
  cyberGridGroup.clear();

  // Futuristic Ground Matrix Grid
  const gridHelper = new THREE.GridHelper(160, 32, 0x06b6d4, 0x1e293b);
  gridHelper.position.y = -8;
  cyberGridGroup.add(gridHelper);

  // 5 Multi-track Neon Rails
  const trackXOffsets = [-48, -24, 0, 24, 48];
  const trackColors = [0x38bdf8, 0x10b981, 0xa855f7, 0x6366f1, 0xf59e0b];

  trackXOffsets.forEach((xPos, idx) => {
    const railPoints = [
      new THREE.Vector3(xPos, -2, -75),
      new THREE.Vector3(xPos, -2, 75)
    ];
    const railGeom = new THREE.BufferGeometry().setFromPoints(railPoints);
    const railMat = new THREE.LineBasicMaterial({
      color: trackColors[idx],
      linewidth: 3,
      transparent: true,
      opacity: 0.75
    });
    const railLine = new THREE.Line(railGeom, railMat);
    cyberGridGroup.add(railLine);
  });

  cyberGridGroup.visible = false;
}

// Build Scenery for Pyramid Ascension Mode
function buildPyramidRingsScenery() {
  pyramidRingsGroup.clear();

  const tiers = [
    { y: -35, radius: 55, color: 0x38bdf8 },
    { y: -15, radius: 42, color: 0x10b981 },
    { y: 5,   radius: 30, color: 0xa855f7 },
    { y: 25,  radius: 18, color: 0x6366f1 },
    { y: 45,  radius: 9,  color: 0xf59e0b }
  ];

  tiers.forEach(tier => {
    const ringGeom = new THREE.RingGeometry(tier.radius - 0.4, tier.radius + 0.4, 64);
    const ringMat = new THREE.MeshBasicMaterial({
      color: tier.color,
      side: THREE.DoubleSide,
      transparent: true,
      opacity: 0.5
    });
    const ringMesh = new THREE.Mesh(ringGeom, ringMat);
    ringMesh.rotation.x = Math.PI / 2;
    ringMesh.position.y = tier.y;
    pyramidRingsGroup.add(ringMesh);
  });

  // Central Vertical Summit Laser Beam
  const beamPoints = [new THREE.Vector3(0, -45, 0), new THREE.Vector3(0, 70, 0)];
  const beamGeom = new THREE.BufferGeometry().setFromPoints(beamPoints);
  const beamMat = new THREE.LineBasicMaterial({ color: 0xf59e0b, transparent: true, opacity: 0.65 });
  const beamLine = new THREE.Line(beamGeom, beamMat);
  pyramidRingsGroup.add(beamLine);

  pyramidRingsGroup.visible = false;
}

// Compute all 4 spatial coordinate layouts for each of the 100 days
function buildAllNodePositions() {
  nodesGroup.clear();
  linesGroup.clear();
  labelsGroup.clear();
  dependencyLinesGroup.clear();
  dayNodesMap.clear();

  const totalDays = dayDataList.length > 0 ? dayDataList.length : 100;

  // Graph Cluster Centers
  const graphClusters = {
    1: new THREE.Vector3(-52, -18, -15),
    2: new THREE.Vector3(-22, 22, 35),
    3: new THREE.Vector3(42, 28, -25),
    4: new THREE.Vector3(50, -22, 25),
    5: new THREE.Vector3(0, 52, 0)
  };

  // Cyber Track X Offsets
  const cyberTrackX = { 1: -48, 2: -24, 3: 0, 4: 24, 5: 48 };

  // Pyramid Tiers
  const pyramidTiers = {
    1: { y: -35, radius: 55 },
    2: { y: -15, radius: 42 },
    3: { y: 5,   radius: 30 },
    4: { y: 25,  radius: 18 },
    5: { y: 45,  radius: 9 }
  };

  // Phase Day Ranges
  const phaseRanges = {
    1: [1, 25],
    2: [26, 50],
    3: [51, 75],
    4: [76, 90],
    5: [91, 100]
  };

  const helixPoints = [];

  for (let i = 0; i < totalDays; i++) {
    const dayData = dayDataList[i] || { dayNumber: i + 1, title: `Day ${i + 1}`, status: 'Planned' };
    const dayNum = dayData.dayNumber || (i + 1);
    const phaseId = getPhaseFromDayNumber(dayNum);
    const range = phaseRanges[phaseId];
    const indexInPhase = dayNum - range[0];
    const totalInPhase = range[1] - range[0] + 1;

    // 1. HELIX COORDINATES
    const t = i / (totalDays - 1);
    const turns = 4.5;
    const angle = t * Math.PI * 2 * turns;
    const yHelix = -45 + t * 95;
    const rHelix = 32 + Math.sin(t * Math.PI) * 16;
    const helixPos = new THREE.Vector3(Math.cos(angle) * rHelix, yHelix, Math.sin(angle) * rHelix);
    helixPoints.push(helixPos);

    // 2. NEURAL GRAPH COORDINATES (Clustered Fibonacci Sphere)
    const center = graphClusters[phaseId];
    const phi = Math.acos(1 - 2 * (indexInPhase + 0.5) / totalInPhase);
    const goldenRatio = 1.61803398875;
    const theta = 2 * Math.PI * indexInPhase / goldenRatio;
    const rGraph = 14 + (indexInPhase % 4) * 2.5;
    const graphPos = new THREE.Vector3(
      center.x + rGraph * Math.sin(phi) * Math.cos(theta),
      center.y + rGraph * Math.sin(phi) * Math.sin(theta),
      center.z + rGraph * Math.cos(phi)
    );

    // 3. CYBER METRO GRID COORDINATES
    const tTrack = indexInPhase / Math.max(1, totalInPhase - 1);
    const zCyber = -68 + tTrack * 136;
    const xCyber = cyberTrackX[phaseId];
    const yCyber = (phaseId === 5 ? 12 : (phaseId === 3 ? 8 : 4));
    const cyberPos = new THREE.Vector3(xCyber, yCyber, zCyber);

    // 4. SKILL PYRAMID COORDINATES
    const tier = pyramidTiers[phaseId];
    const anglePyramid = (indexInPhase / totalInPhase) * Math.PI * 2;
    const pyramidPos = new THREE.Vector3(
      Math.cos(anglePyramid) * tier.radius,
      tier.y,
      Math.sin(anglePyramid) * tier.radius
    );

    // Create 3D Day Sphere Node
    const statusColor = STATUS_COLORS[dayData.status] || STATUS_COLORS.Planned;
    const isMilestone = [25, 50, 75, 90, 100].includes(dayNum);
    const sphereRadius = isMilestone ? 2.8 : (dayNum === 1 ? 2.4 : 1.8);

    const sphereGeom = new THREE.SphereGeometry(sphereRadius, 24, 24);
    const sphereMat = new THREE.MeshStandardMaterial({
      color: statusColor,
      emissive: statusColor,
      emissiveIntensity: dayData.status === 'InProgress' ? 0.75 : (dayData.status === 'Completed' ? 0.55 : 0.25),
      roughness: 0.2,
      metalness: 0.8
    });

    const nodeMesh = new THREE.Mesh(sphereGeom, sphereMat);
    nodeMesh.position.copy(helixPos);
    nodeMesh.userData = { dayData, dayNum, phaseId, isMilestone };

    // Glowing orbit ring for Milestone days and InProgress days
    if (isMilestone || dayData.status === 'InProgress') {
      const ringGeom = new THREE.RingGeometry(sphereRadius * 1.35, sphereRadius * 1.65, 32);
      const ringMat = new THREE.MeshBasicMaterial({
        color: statusColor,
        side: THREE.DoubleSide,
        transparent: true,
        opacity: 0.65
      });
      const ringMesh = new THREE.Mesh(ringGeom, ringMat);
      ringMesh.rotation.x = Math.PI / 2;
      nodeMesh.add(ringMesh);
    }

    nodesGroup.add(nodeMesh);

    dayNodesMap.set(dayNum, {
      mesh: nodeMesh,
      data: dayData,
      position: nodeMesh.position,
      targetPosition: helixPos.clone(),
      helixPos,
      graphPos,
      cyberPos,
      pyramidPos,
      initialScale: nodeMesh.scale.clone()
    });

    // Milestone Labels in 3D Space
    if (dayNum === 1 || isMilestone) {
      addPhase3DLabel(helixPos, dayNum);
    }
  }

  // Build Glowing Tube Spline for Helix Mode
  if (helixPoints.length > 1) {
    const curvePath = new THREE.CatmullRomCurve3(helixPoints);
    const tubeGeom = new THREE.TubeGeometry(curvePath, 250, 0.45, 8, false);
    const tubeMat = new THREE.MeshStandardMaterial({
      color: 0x38bdf8,
      emissive: 0x1e293b,
      roughness: 0.4,
      metalness: 0.8,
      transparent: true,
      opacity: 0.75
    });
    const tubeMesh = new THREE.Mesh(tubeGeom, tubeMat);
    linesGroup.add(tubeMesh);
    buildMomentumParticleStream(curvePath);
  }

  // Build Cross-Topic Knowledge Dependency Synapses for Graph Mode
  buildKnowledgeGraphSynapses();
}

// Cross-topic conceptual dependencies linking domains in 3D Neural Graph
function buildKnowledgeGraphSynapses() {
  dependencyLinesGroup.clear();

  // Meaningful architectural dependencies
  const dependencies = [
    [1, 15],  // Memory -> SQL
    [15, 29], // SQL -> RabbitMQ
    [27, 34], // YARP -> OpenTelemetry
    [29, 31], // RabbitMQ -> Outbox Pattern
    [31, 32], // Outbox -> Saga Pattern
    [34, 68], // OpenTelemetry -> AI Observability
    [41, 43], // Docker -> Kubernetes
    [43, 81], // Kubernetes -> Helm
    [55, 56], // Embeddings -> pgvector
    [56, 59], // pgvector -> RAG
    [59, 61], // RAG -> Tool Calling
    [61, 63], // Tool Calling -> AI Agents
    [63, 65], // Agents -> Multi-Agent Systems
    [63, 73], // Agents -> Coding Agent Architecture
    [56, 78], // pgvector -> Capstone Ingestion Pipeline
    [61, 79], // Tool Calling -> Capstone RAG & Cache
    [43, 80], // Kubernetes -> Capstone Observability
    [72, 92], // AI System Design -> High-Scale System Design
    [75, 95]  // AI Review -> System Design Interview
  ];

  dependencies.forEach(([fromDay, toDay]) => {
    const fromEntry = dayNodesMap.get(fromDay);
    const toEntry = dayNodesMap.get(toDay);
    if (fromEntry && toEntry) {
      const lineGeom = new THREE.BufferGeometry().setFromPoints([fromEntry.graphPos, toEntry.graphPos]);
      const lineMat = new THREE.LineBasicMaterial({
        color: 0xa855f7,
        transparent: true,
        opacity: 0.45
      });
      const line = new THREE.Line(lineGeom, lineMat);
      line.userData = { fromDay, toDay };
      dependencyLinesGroup.add(line);
    }
  });

  dependencyLinesGroup.visible = false;
}

// Particle Stream traveling along the 3D curve
function buildMomentumParticleStream(curve) {
  particlesGroup.clear();
  const particleCount = 60;
  const geometry = new THREE.BufferGeometry();
  const positions = new Float32Array(particleCount * 3);

  for (let i = 0; i < particleCount; i++) {
    const pt = curve.getPoint(i / particleCount);
    positions[i * 3] = pt.x;
    positions[i * 3 + 1] = pt.y;
    positions[i * 3 + 2] = pt.z;
  }

  geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));

  const material = new THREE.PointsMaterial({
    color: 0x60a5fa,
    size: 2.8,
    transparent: true,
    opacity: 0.9
  });

  const particlePoints = new THREE.Points(geometry, material);
  particlePoints.userData = { curve };
  particlesGroup.add(particlePoints);
}

// Adds high-legibility canvas sprite labels in 3D space
function addPhase3DLabel(pos, dayNum) {
  let labelText = `Day ${dayNum}`;
  let subText = "";
  if (dayNum === 1) { labelText = "START • Day 1"; subText = "Advanced C#"; }
  if (dayNum === 25) { labelText = "MILESTONE • Day 25"; subText = "Phase 1 Complete"; }
  if (dayNum === 50) { labelText = "MILESTONE • Day 50"; subText = "Phase 2 Complete"; }
  if (dayNum === 75) { labelText = "MILESTONE • Day 75"; subText = "Phase 3 Complete"; }
  if (dayNum === 90) { labelText = "MILESTONE • Day 90"; subText = "Phase 4 Complete"; }
  if (dayNum === 100) { labelText = "SUMMIT • Day 100"; subText = "Mastery Achieved!"; }

  const canvas = document.createElement('canvas');
  canvas.width = 256;
  canvas.height = 80;
  const ctx = canvas.getContext('2d');

  ctx.fillStyle = "rgba(15, 23, 42, 0.85)";
  ctx.roundRect(4, 4, 248, 72, 12);
  ctx.fill();
  ctx.strokeStyle = "rgba(56, 189, 248, 0.5)";
  ctx.lineWidth = 2;
  ctx.stroke();

  ctx.font = "bold 20px -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
  ctx.fillStyle = "#ffffff";
  ctx.textAlign = "center";
  ctx.fillText(labelText, 128, 34);

  ctx.font = "14px -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
  ctx.fillStyle = "#38bdf8";
  ctx.fillText(subText, 128, 58);

  const texture = new THREE.CanvasTexture(canvas);
  const spriteMaterial = new THREE.SpriteMaterial({ map: texture, transparent: true });
  const sprite = new THREE.Sprite(spriteMaterial);

  sprite.position.set(pos.x + 6, pos.y + 3.5, pos.z);
  sprite.scale.set(16, 5, 1);
  labelsGroup.add(sprite);
}

// Smoothly morphs all 100 nodes into a different 3D layout architecture!
function switch3DLayout(mode, immediate = false) {
  current3DMode = mode;

  // Update layout buttons UI
  const modes = ['helix', 'graph', 'cyber', 'pyramid'];
  modes.forEach(m => {
    const btn = document.getElementById(`btn-layout-${m}`);
    if (btn) {
      if (m === mode) {
        btn.className = "px-3 py-1.5 rounded-lg text-xs font-bold transition bg-blue-600 text-white shadow";
      } else {
        btn.className = "px-3 py-1.5 rounded-lg text-xs font-medium text-slate-300 hover:text-white hover:bg-slate-800 transition";
      }
    }
  });

  // Assign target positions for all 100 nodes
  dayNodesMap.forEach(entry => {
    let target;
    if (mode === 'helix') target = entry.helixPos;
    else if (mode === 'graph') target = entry.graphPos;
    else if (mode === 'cyber') target = entry.cyberPos;
    else if (mode === 'pyramid') target = entry.pyramidPos;
    else target = entry.helixPos;

    entry.targetPosition.copy(target);
    if (immediate) {
      entry.mesh.position.copy(target);
    }
  });

  // Toggle mode-specific scenery
  if (linesGroup) linesGroup.visible = (mode === 'helix');
  if (particlesGroup) particlesGroup.visible = (mode === 'helix');
  if (dependencyLinesGroup) dependencyLinesGroup.visible = (mode === 'graph');
  if (cyberGridGroup) cyberGridGroup.visible = (mode === 'cyber');
  if (pyramidRingsGroup) pyramidRingsGroup.visible = (mode === 'pyramid');

  // Smooth camera transitions tailored for each perspective
  if (!immediate && camera3D && controls3D) {
    if (mode === 'helix') {
      targetCameraPos = new THREE.Vector3(0, 30, 160);
      targetLookAt = new THREE.Vector3(0, 5, 0);
    } else if (mode === 'graph') {
      targetCameraPos = new THREE.Vector3(0, 35, 170);
      targetLookAt = new THREE.Vector3(0, 15, 0);
    } else if (mode === 'cyber') {
      targetCameraPos = new THREE.Vector3(0, 65, 130);
      targetLookAt = new THREE.Vector3(0, 0, 0);
    } else if (mode === 'pyramid') {
      targetCameraPos = new THREE.Vector3(0, 35, 150);
      targetLookAt = new THREE.Vector3(0, 10, 0);
    }
  }

  // Update mode badge in title
  const modeTitles = {
    helix: "Cosmic Helix Track",
    graph: "Neural Knowledge Graph & Synapses",
    cyber: "Cyberpunk Subway Grid",
    pyramid: "Skill Ascension Pyramid"
  };
  const titleEl = document.getElementById('roadmap-3d-mode-title');
  if (titleEl) titleEl.textContent = modeTitles[mode] || "3D Constellation";
}

// Refresh node materials and colors when data updates
function update3DNodes() {
  if (!dayNodesMap || dayNodesMap.size === 0) return;

  dayDataList.forEach(day => {
    const nodeObj = dayNodesMap.get(day.dayNumber);
    if (nodeObj && nodeObj.mesh) {
      nodeObj.data = day;
      const col = STATUS_COLORS[day.status] || STATUS_COLORS.Planned;
      nodeObj.mesh.material.color.setHex(col);
      nodeObj.mesh.material.emissive.setHex(col);
      nodeObj.mesh.material.emissiveIntensity = day.status === 'InProgress' ? 0.75 : (day.status === 'Completed' ? 0.55 : 0.25);
    }
  });
}

// Animation loop
function animate3D() {
  animationFrameId = requestAnimationFrame(animate3D);

  if (controls3D) {
    controls3D.update();
  }

  // Smooth position interpolation for all 100 nodes when morphing between layouts
  dayNodesMap.forEach(entry => {
    if (entry.mesh.position.distanceTo(entry.targetPosition) > 0.05) {
      entry.mesh.position.lerp(entry.targetPosition, 0.08);
    }
  });

  // Animate pulse particles along spline (in Helix mode)
  if (current3DMode === 'helix' && particlesGroup && particlesGroup.children.length > 0) {
    const particlePoints = particlesGroup.children[0];
    const curve = particlePoints.userData.curve;
    if (curve) {
      pulseProgress = (pulseProgress + 0.0012) % 1.0;
      const positions = particlePoints.geometry.attributes.position.array;
      const count = positions.length / 3;

      for (let i = 0; i < count; i++) {
        const t = (pulseProgress + i / count) % 1.0;
        const pt = curve.getPoint(t);
        positions[i * 3] = pt.x;
        positions[i * 3 + 1] = pt.y;
        positions[i * 3 + 2] = pt.z;
      }
      particlePoints.geometry.attributes.position.needsUpdate = true;
    }
  }

  // Smooth camera interpolation towards target presets
  if (targetCameraPos && camera3D) {
    camera3D.position.lerp(targetCameraPos, 0.05);
    if (targetLookAt && controls3D) {
      controls3D.target.lerp(targetLookAt, 0.05);
    }
    if (camera3D.position.distanceTo(targetCameraPos) < 0.5) {
      targetCameraPos = null;
      targetLookAt = null;
    }
  }

  // Gentle floating animation on hovered node
  if (hoveredNode) {
    hoveredNode.rotation.y += 0.03;
  }

  if (renderer3D && scene3D && camera3D) {
    renderer3D.render(scene3D, camera3D);
  }
}

// Raycasting: Mouse Move
function on3DMouseMove(event) {
  const rect = renderer3D.domElement.getBoundingClientRect();
  mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
  mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

  raycaster.setFromCamera(mouse, camera3D);
  const intersects = raycaster.intersectObjects(nodesGroup.children, false);

  if (intersects.length > 0) {
    const mesh = intersects[0].object;
    if (hoveredNode !== mesh) {
      if (hoveredNode) resetHoveredNode();
      hoveredNode = mesh;
      hoveredNode.scale.set(1.45, 1.45, 1.45);
      show3DTooltip(event.clientX, event.clientY, mesh.userData.dayData);
      renderer3D.domElement.style.cursor = 'pointer';
    } else {
      update3DTooltipPosition(event.clientX, event.clientY);
    }
  } else {
    if (hoveredNode) {
      resetHoveredNode();
      hide3DTooltip();
      renderer3D.domElement.style.cursor = 'default';
    }
  }
}

function resetHoveredNode() {
  if (!hoveredNode) return;
  const entry = dayNodesMap.get(hoveredNode.userData.dayNum);
  if (entry) {
    hoveredNode.scale.copy(entry.initialScale);
  } else {
    hoveredNode.scale.set(1, 1, 1);
  }
  hoveredNode = null;
}

// Raycasting: Mouse Click
function on3DMouseClick(event) {
  const rect = renderer3D.domElement.getBoundingClientRect();
  mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
  mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

  raycaster.setFromCamera(mouse, camera3D);
  const intersects = raycaster.intersectObjects(nodesGroup.children, false);

  if (intersects.length > 0) {
    const mesh = intersects[0].object;
    selectDayNode(mesh.userData.dayData, mesh.position);
  }
}

function on3DTouchStart(event) {
  if (event.touches.length === 1) {
    const touch = event.touches[0];
    const rect = renderer3D.domElement.getBoundingClientRect();
    mouse.x = ((touch.clientX - rect.left) / rect.width) * 2 - 1;
    mouse.y = -((touch.clientY - rect.top) / rect.height) * 2 + 1;

    raycaster.setFromCamera(mouse, camera3D);
    const intersects = raycaster.intersectObjects(nodesGroup.children, false);

    if (intersects.length > 0) {
      const mesh = intersects[0].object;
      selectDayNode(mesh.userData.dayData, mesh.position);
    }
  }
}

// Select Node & Open 3D Inspector Drawer
function selectDayNode(day, position) {
  selectedNode = day;
  open3DInspector(day);

  // Smoothly glide camera toward the node
  if (position) {
    const offset = new THREE.Vector3(0, 8, 28);
    targetCameraPos = position.clone().add(offset);
    targetLookAt = position.clone();
    if (controls3D) controls3D.autoRotate = false;
  }
}

// Hover Tooltip Display
function show3DTooltip(clientX, clientY, day) {
  const tooltip = document.getElementById('roadmap-3d-tooltip');
  if (!tooltip || !day) return;

  const phaseNum = getPhaseFromDayNumber(day.dayNumber);
  tooltip.innerHTML = `
    <div class="flex items-center gap-2">
      <span class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-blue-950 text-blue-300 border border-blue-800">Day ${day.dayNumber}</span>
      <span class="text-[10px] text-slate-400 font-mono">Phase ${phaseNum}</span>
      <span class="badge-status ml-auto ${getStatusBadgeClass(day.status)} text-[10px] py-0 px-1.5">${getStatusIcon(day.status)} ${formatStatusName(day.status)}</span>
    </div>
    <div class="font-bold text-white text-xs mt-1 truncate">${escapeHtml(day.title)}</div>
    <div class="text-[11px] text-slate-400 mt-0.5 truncate">${escapeHtml(day.theme)}</div>
    <div class="mt-1.5 flex items-center justify-between text-[10px] text-slate-500 border-t border-slate-800 pt-1">
      <span>${(day.tasks || []).length} Tasks</span>
      <span class="text-blue-400 font-semibold">Click to Inspect ➔</span>
    </div>
  `;

  tooltip.classList.remove('hidden');
  update3DTooltipPosition(clientX, clientY);
}

function update3DTooltipPosition(clientX, clientY) {
  const tooltip = document.getElementById('roadmap-3d-tooltip');
  if (!tooltip) return;
  const x = Math.min(window.innerWidth - 260, clientX + 16);
  const y = Math.min(window.innerHeight - 120, clientY + 16);
  tooltip.style.left = `${x}px`;
  tooltip.style.top = `${y}px`;
}

function hide3DTooltip() {
  const tooltip = document.getElementById('roadmap-3d-tooltip');
  if (tooltip) tooltip.classList.add('hidden');
}

// Floating Inspector Drawer
function open3DInspector(day) {
  const drawer = document.getElementById('roadmap-3d-inspector');
  if (!drawer || !day) return;

  const phaseNum = getPhaseFromDayNumber(day.dayNumber);
  const phaseName = getPhaseName(phaseNum);

  document.getElementById('insp3d-daynum').textContent = `Day ${day.dayNumber}`;
  document.getElementById('insp3d-title').textContent = day.title;
  document.getElementById('insp3d-theme').textContent = `${day.theme} • Date: ${day.calendarDate}`;
  document.getElementById('insp3d-phase').textContent = `Phase ${phaseNum}: ${phaseName}`;

  const badge = document.getElementById('insp3d-status-badge');
  badge.className = `badge-status ${getStatusBadgeClass(day.status)}`;
  badge.innerHTML = `${getStatusIcon(day.status)} ${formatStatusName(day.status)}`;

  const tasksList = document.getElementById('insp3d-tasks-list');
  if (!day.tasks || day.tasks.length === 0) {
    tasksList.innerHTML = `<div class="text-xs text-slate-500 py-2">No tasks assigned to Day ${day.dayNumber}.</div>`;
  } else {
    tasksList.innerHTML = day.tasks.map(t => `
      <div class="flex items-start justify-between p-2.5 rounded-lg border border-slate-800 bg-slate-900/60 text-xs">
        <div class="flex items-start gap-2 flex-1">
          <input type="checkbox" ${t.status === 'Completed' ? 'checked' : ''} onchange="toggleTaskStatusFrom3D(${t.id}, this.checked, ${day.dayNumber})" class="mt-0.5 w-3.5 h-3.5 rounded text-blue-600 bg-slate-950 border-slate-700 cursor-pointer">
          <div>
            <div class="${t.status === 'Completed' ? 'line-through text-slate-500' : 'text-slate-200 font-medium'}">
              ${escapeHtml(t.title)}
            </div>
            <div class="flex items-center gap-2 mt-1">
              <span class="text-[9px] uppercase font-bold px-1.5 py-0.2 rounded bg-slate-800 text-slate-400 border border-slate-700">${t.category}</span>
              <span class="text-[10px] text-slate-500">⏱️ ${t.estimatedMinutes}m</span>
            </div>
          </div>
        </div>
      </div>
    `).join('');
  }

  // Open button handler
  document.getElementById('insp3d-btn-openday').onclick = () => {
    openDayDetail(day.dayNumber);
  };

  drawer.classList.remove('hidden');
}

function close3DInspector() {
  const drawer = document.getElementById('roadmap-3d-inspector');
  if (drawer) drawer.classList.add('hidden');
  selectedNode = null;
}

// Toggle Task Status directly from 3D Inspector
async function toggleTaskStatusFrom3D(taskId, isCompleted, dayNumber) {
  try {
    await apiCall(`/api/tasks/${taskId}/status`, 'PUT', isCompleted ? 'Completed' : 'Pending');
    showToast(isCompleted ? "Task marked completed! 🎉" : "Task reset to pending");

    // Refresh local cache and 3D node
    allDaysCache = await apiCall('/api/days');
    dayDataList = allDaysCache.sort((a, b) => (a.dayNumber || 0) - (b.dayNumber || 0));
    update3DNodes();
    update3DHUDStats();

    // Refresh inspector with updated data
    const updatedDay = allDaysCache.find(d => d.dayNumber === dayNumber);
    if (updatedDay) {
      open3DInspector(updatedDay);
    }
  } catch (err) {
    showToast("Failed to update task status", "error");
  }
}

// Update Top-Right HUD Stats
function update3DHUDStats() {
  const hudTotal = document.getElementById('hud3d-total');
  const hudCompleted = document.getElementById('hud3d-completed');
  const hudStreak = document.getElementById('hud3d-streak');

  if (hudTotal) hudTotal.textContent = dayDataList.length || 100;
  if (hudCompleted) {
    const done = dayDataList.filter(d => d.status === 'Completed').length;
    hudCompleted.textContent = `${done} (${Math.round((done / 100) * 100)}%)`;
  }
  if (hudStreak) {
    const currentStreak = document.getElementById('metric-current-streak')?.textContent || "0";
    hudStreak.textContent = currentStreak;
  }
}

// Camera Preset Views
function reset3DCameraView() {
  if (current3DMode === 'helix') {
    targetCameraPos = new THREE.Vector3(0, 30, 160);
    targetLookAt = new THREE.Vector3(0, 5, 0);
  } else if (current3DMode === 'graph') {
    targetCameraPos = new THREE.Vector3(0, 35, 170);
    targetLookAt = new THREE.Vector3(0, 15, 0);
  } else if (current3DMode === 'cyber') {
    targetCameraPos = new THREE.Vector3(0, 65, 130);
    targetLookAt = new THREE.Vector3(0, 0, 0);
  } else {
    targetCameraPos = new THREE.Vector3(0, 35, 150);
    targetLookAt = new THREE.Vector3(0, 10, 0);
  }
  if (controls3D) controls3D.autoRotate = isAutoRotating;
  close3DInspector();
}

function focusOnTodayNode() {
  const today = dayDataList.find(d => d.status === 'InProgress') ||
                dayDataList.find(d => d.status === 'Planned') ||
                dayDataList[0];

  if (today && dayNodesMap.has(today.dayNumber)) {
    const entry = dayNodesMap.get(today.dayNumber);
    selectDayNode(today, entry.mesh.position);
  }
}

function focusOnPhase(phaseId) {
  const phaseRanges = {
    1: [1, 25],
    2: [26, 50],
    3: [51, 75],
    4: [76, 90],
    5: [91, 100]
  };

  const range = phaseRanges[phaseId] || [1, 25];
  const midDay = Math.floor((range[0] + range[1]) / 2);

  if (dayNodesMap.has(midDay)) {
    const entry = dayNodesMap.get(midDay);
    const offset = new THREE.Vector3(0, 15, 55);
    targetCameraPos = entry.mesh.position.clone().add(offset);
    targetLookAt = entry.mesh.position.clone();
    if (controls3D) controls3D.autoRotate = false;
  }
}

function toggle3DAutoRotate() {
  isAutoRotating = !isAutoRotating;
  if (controls3D) controls3D.autoRotate = isAutoRotating;
  const btn = document.getElementById('btn-3d-autorotate');
  if (btn) {
    btn.classList.toggle('text-blue-400', isAutoRotating);
    btn.classList.toggle('bg-blue-950/60', isAutoRotating);
  }
  showToast(isAutoRotating ? "Auto-rotation enabled" : "Auto-rotation paused");
}

function toggle3DLabels() {
  if (!labelsGroup) return;
  labelsGroup.visible = !labelsGroup.visible;
  const btn = document.getElementById('btn-3d-labels');
  if (btn) {
    btn.classList.toggle('text-blue-400', labelsGroup.visible);
    btn.classList.toggle('bg-blue-950/60', labelsGroup.visible);
  }
}

function filter3DNodes(statusFilter) {
  dayNodesMap.forEach(({ mesh, data, initialScale }) => {
    if (statusFilter === 'all' || data.status.toLowerCase() === statusFilter.toLowerCase()) {
      mesh.visible = true;
      mesh.scale.copy(initialScale);
    } else {
      mesh.visible = false;
    }
  });
}

function on3DResize() {
  const container = document.getElementById('roadmap-3d-canvas-container');
  if (!container || !renderer3D || !camera3D) return;

  const width = container.clientWidth;
  const height = container.clientHeight;

  camera3D.aspect = width / height;
  camera3D.updateProjectionMatrix();
  renderer3D.setSize(width, height);
}

// Utility Helpers
function getPhaseFromDayNumber(dayNum) {
  if (dayNum <= 25) return 1;
  if (dayNum <= 50) return 2;
  if (dayNum <= 75) return 3;
  if (dayNum <= 90) return 4;
  return 5;
}

function getPhaseName(phaseId) {
  switch (phaseId) {
    case 1: return "Advanced C# & .NET Core Internals";
    case 2: return "Microservices & Cloud-Native Systems";
    case 3: return "Applied AI & LLM Engineering";
    case 4: return "Production AI Capstone Portfolio";
    case 5: return "High-Scale System Design & Mastery";
    default: return "Core Engineering";
  }
}
