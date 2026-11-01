import { onCleanup, onMount } from 'solid-js';

// matches the ring ellipses (192 / 700) so the spin axis is perpendicular to the orbits
const AXIS_PITCH = (16 * Math.PI) / 180;
const AXIS_ROLL = (16 * Math.PI) / 180;
const PITCH_COS = Math.cos(AXIS_PITCH);
const PITCH_SIN = Math.sin(AXIS_PITCH);
const ROLL_COS = Math.cos(AXIS_ROLL);
const ROLL_SIN = Math.sin(AXIS_ROLL);
const SECONDS_PER_TURN = 70;
const ROW_STEP_DEGREES = 2.4;
const LIGHT = normalize(-0.55, 0.62, 0.56);
const INTENSITY_BUCKETS = 10;
const BEACON_PERIOD = 3.2;

interface SpinAngle {
  cos: number;
  sin: number;
}

interface Beacon {
  lat: number;
  lon: number;
  color: string;
}

const beacons: Beacon[] = [
  { lat: 24, lon: 18, color: '240 171 252' },
  { lat: -12, lon: 96, color: '134 239 172' },
  { lat: 38, lon: 172, color: '125 211 252' },
  { lat: -30, lon: 248, color: '253 230 138' },
  { lat: 8, lon: 318, color: '196 181 253' },
];

function normalize(x: number, y: number, z: number): [number, number, number] {
  const length = Math.hypot(x, y, z);

  return [x / length, y / length, z / length];
}

function hash(x: number, y: number, z: number) {
  let h = Math.imul(x, 374761393) ^ Math.imul(y, 668265263) ^ Math.imul(z, 1274126177);
  h = Math.imul(h ^ (h >>> 13), 1274126177);

  return ((h ^ (h >>> 16)) >>> 0) / 4294967296;
}

function smooth(t: number) {
  return t * t * (3 - 2 * t);
}

function lerp(a: number, b: number, t: number) {
  return a + (b - a) * t;
}

function valueNoise(x: number, y: number, z: number) {
  const xi = Math.floor(x);
  const yi = Math.floor(y);
  const zi = Math.floor(z);
  const u = smooth(x - xi);
  const v = smooth(y - yi);
  const w = smooth(z - zi);

  const x00 = lerp(hash(xi, yi, zi), hash(xi + 1, yi, zi), u);
  const x10 = lerp(hash(xi, yi + 1, zi), hash(xi + 1, yi + 1, zi), u);
  const x01 = lerp(hash(xi, yi, zi + 1), hash(xi + 1, yi, zi + 1), u);
  const x11 = lerp(hash(xi, yi + 1, zi + 1), hash(xi + 1, yi + 1, zi + 1), u);

  return lerp(lerp(x00, x10, v), lerp(x01, x11, v), w);
}

function isLand(x: number, y: number, z: number) {
  let total = 0;
  let amplitude = 0.5;
  let frequency = 1.7;

  for (let octave = 0; octave < 4; octave++) {
    total += amplitude * valueNoise(x * frequency + 11.3, y * frequency + 4.7, z * frequency - 7.9);
    amplitude /= 2;
    frequency *= 2.1;
  }

  return total > 0.47;
}

// body-space unit vectors, y is the spin axis
function buildDots() {
  const points: number[] = [];

  for (let lat = -90 + ROW_STEP_DEGREES / 2; lat < 90; lat += ROW_STEP_DEGREES) {
    const latRad = (lat * Math.PI) / 180;
    const ring = Math.cos(latRad);
    const count = Math.max(1, Math.round((360 / ROW_STEP_DEGREES) * ring));

    for (let i = 0; i < count; i++) {
      const lonRad = (i / count) * Math.PI * 2;
      const x = ring * Math.cos(lonRad);
      const y = Math.sin(latRad);
      const z = ring * Math.sin(lonRad);

      const onLand = isLand(x, y, z);

      if (onLand) {
        points.push(x, y, z);
      }
    }
  }

  return new Float32Array(points);
}

function toBody(latDegrees: number, lonDegrees: number): [number, number, number] {
  const lat = (latDegrees * Math.PI) / 180;
  const lon = (lonDegrees * Math.PI) / 180;

  return [Math.cos(lat) * Math.cos(lon), Math.sin(lat), Math.cos(lat) * Math.sin(lon)];
}

// returns view-space coordinates: x right, y up, z towards the viewer
function project(x: number, y: number, z: number, spin: SpinAngle, out: Float64Array) {
  const x1 = x * spin.cos - z * spin.sin;
  const z1 = x * spin.sin + z * spin.cos;

  const y2 = y * PITCH_COS - z1 * PITCH_SIN;
  const z2 = y * PITCH_SIN + z1 * PITCH_COS;

  out[0] = x1 * ROLL_COS - y2 * ROLL_SIN;
  out[1] = x1 * ROLL_SIN + y2 * ROLL_COS;
  out[2] = z2;
}

function addDot(path: Path2D, cx: number, cy: number, r: number, v: Float64Array, radius: number) {
  const sx = cx + v[0] * r;
  const sy = cy - v[1] * r;
  const squash = Math.max(Math.abs(v[2]), 0.12);
  const angle = Math.atan2(-v[1], v[0]);

  path.moveTo(sx + Math.cos(angle) * radius * squash, sy + Math.sin(angle) * radius * squash);
  path.ellipse(sx, sy, radius * squash, radius, angle, 0, Math.PI * 2);
}

function drawGraticule(
  ctx: CanvasRenderingContext2D,
  cx: number,
  cy: number,
  r: number,
  spin: SpinAngle,
) {
  const v = new Float64Array(3);
  const path = new Path2D();

  const trace = (pointAt: (t: number) => [number, number, number]) => {
    let drawing = false;

    for (let t = 0; t <= 360; t += 3) {
      const [x, y, z] = pointAt(t);
      project(x, y, z, spin, v);

      if (v[2] <= 0) {
        drawing = false;
        continue;
      }

      const sx = cx + v[0] * r;
      const sy = cy - v[1] * r;

      if (drawing) {
        path.lineTo(sx, sy);
      } else {
        path.moveTo(sx, sy);
        drawing = true;
      }
    }
  };

  for (let lon = 0; lon < 180; lon += 30) {
    trace((t) => toBody(t - 180, lon));
  }

  for (let lat = -60; lat <= 60; lat += 30) {
    trace((t) => toBody(lat, t));
  }

  ctx.strokeStyle = 'rgb(196 181 253 / 0.09)';
  ctx.lineWidth = 1;
  ctx.stroke(path);
}

export default function HeroGlobe() {
  let canvas!: HTMLCanvasElement;

  onMount(() => {
    const ctx = canvas.getContext('2d');

    if (!ctx) {
      return;
    }

    const dots = buildDots();
    const beaconPoints = beacons.map((beacon) => toBody(beacon.lat, beacon.lon));
    const motionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    const v = new Float64Array(3);

    let size = 0;
    let frame = 0;
    let visible = true;
    let last = 0;
    let spin = 0.6;

    const resize = () => {
      const ratio = Math.min(window.devicePixelRatio || 1, 2);
      size = canvas.clientWidth;
      canvas.width = Math.round(size * ratio);
      canvas.height = Math.round(size * ratio);
      ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
    };

    const draw = (time: number) => {
      const cx = size / 2;
      const cy = size / 2;
      const r = size / 2 - 1;
      const dotRadius = Math.max(1, size / 190);
      const angle = { cos: Math.cos(spin), sin: Math.sin(spin) };

      ctx.clearRect(0, 0, size, size);

      const back = new Path2D();
      const front = Array.from({ length: INTENSITY_BUCKETS }, () => new Path2D());

      for (let i = 0; i < dots.length; i += 3) {
        project(dots[i], dots[i + 1], dots[i + 2], angle, v);

        if (v[2] < 0) {
          addDot(back, cx, cy, r, v, dotRadius * 0.8);
          continue;
        }

        const diffuse = Math.max(0, v[0] * LIGHT[0] + v[1] * LIGHT[1] + v[2] * LIGHT[2]);
        const intensity = (0.22 + 0.78 * diffuse) * (0.35 + 0.65 * v[2]);
        const bucket = Math.min(INTENSITY_BUCKETS - 1, Math.floor(intensity * INTENSITY_BUCKETS));
        addDot(front[bucket], cx, cy, r, v, dotRadius);
      }

      ctx.fillStyle = 'rgb(167 139 250 / 0.1)';
      ctx.fill(back);

      drawGraticule(ctx, cx, cy, r, angle);

      front.forEach((path, bucket) => {
        const alpha = 0.12 + (0.88 * (bucket + 0.5)) / INTENSITY_BUCKETS;
        ctx.fillStyle = `rgb(221 208 255 / ${alpha.toFixed(3)})`;
        ctx.fill(path);
      });

      beaconPoints.forEach(([x, y, z], i) => {
        project(x, y, z, angle, v);

        if (v[2] < 0.15) {
          return;
        }

        const color = beacons[i].color;
        const phase = (time / 1000 / BEACON_PERIOD + i / beaconPoints.length) % 1;
        const fade = Math.min(1, (v[2] - 0.15) * 4);

        const halo = new Path2D();
        addDot(halo, cx, cy, r, v, dotRadius * (2 + phase * 9));
        ctx.strokeStyle = `rgb(${color} / ${((1 - phase) * 0.7 * fade).toFixed(3)})`;
        ctx.lineWidth = 1.25;
        ctx.stroke(halo);

        const core = new Path2D();
        addDot(core, cx, cy, r, v, dotRadius * 2.2);
        ctx.fillStyle = `rgb(${color} / ${fade.toFixed(3)})`;
        ctx.shadowColor = `rgb(${color})`;
        ctx.shadowBlur = 12;
        ctx.fill(core);
        ctx.shadowBlur = 0;
      });
    };

    const tick = (time: number) => {
      const elapsed = Math.min(time - last, 100) / 1000;
      last = time;
      spin += (elapsed / SECONDS_PER_TURN) * Math.PI * 2;
      draw(time);
      frame = visible ? requestAnimationFrame(tick) : 0;
    };

    const start = () => {
      if (frame || !visible || motionQuery.matches) {
        return;
      }

      last = performance.now();
      frame = requestAnimationFrame(tick);
    };

    const showStill = () => {
      cancelAnimationFrame(frame);
      frame = 0;
      draw(BEACON_PERIOD * 400);
    };

    const onMotionChange = () => {
      if (motionQuery.matches) {
        showStill();
      } else {
        start();
      }
    };

    resize();
    onMotionChange();
    motionQuery.addEventListener('change', onMotionChange);

    const resizeObserver = new ResizeObserver(() => {
      resize();
      draw(performance.now());
    });
    resizeObserver.observe(canvas);

    const intersectionObserver = new IntersectionObserver(([entry]) => {
      visible = entry.isIntersecting;
      start();
    });
    intersectionObserver.observe(canvas);

    onCleanup(() => {
      cancelAnimationFrame(frame);
      motionQuery.removeEventListener('change', onMotionChange);
      resizeObserver.disconnect();
      intersectionObserver.disconnect();
    });
  });

  return <canvas ref={canvas} class="hero-planet-globe" />;
}
