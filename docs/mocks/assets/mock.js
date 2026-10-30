/* mock.js — tiny runtime for HTML mocks. No framework, no build step.
   - theme: ?theme=light|dark, localStorage, the mock bar toggle, or the `t` key
   - chrome: ?chrome=0 hides the mock bar and notes (used for screenshots)
   - data-state links: elements with [data-set-state] swap data-state on a target
   Everything degrades gracefully when JavaScript is off. */
(function () {
  var root = document.documentElement;
  var params = new URLSearchParams(location.search);

  function readStored(key) { try { return localStorage.getItem(key); } catch (e) { return null; } }
  function store(key, value) { try { localStorage.setItem(key, value); } catch (e) { /* private mode */ } }

  function currentTheme() {
    if (root.dataset.theme === 'dark' || root.dataset.theme === 'light') return root.dataset.theme;
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
  function applyTheme(theme, persist) {
    root.dataset.theme = theme;
    if (persist) store('mock-theme', theme);
    document.querySelectorAll('[data-theme-toggle]').forEach(function (button) {
      button.setAttribute('aria-pressed', theme === 'dark' ? 'true' : 'false');
      button.textContent = theme === 'dark' ? '☀ Light' : '☾ Dark';
      button.setAttribute('aria-label', theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme');
    });
  }

  var requested = params.get('theme');
  if (requested === 'light' || requested === 'dark') applyTheme(requested, false);
  else if (readStored('mock-theme')) applyTheme(readStored('mock-theme'), false);
  else applyTheme(currentTheme(), false);

  if (params.get('chrome') === '0') root.dataset.chrome = 'off';

  document.addEventListener('click', function (event) {
    var toggle = event.target.closest('[data-theme-toggle]');
    if (toggle) { applyTheme(currentTheme() === 'dark' ? 'light' : 'dark', true); return; }
    var chrome = event.target.closest('[data-chrome-toggle]');
    if (chrome) { root.dataset.chrome = root.dataset.chrome === 'off' ? 'on' : 'off'; return; }
    var setter = event.target.closest('[data-set-state]');
    if (setter) {
      var target = document.querySelector(setter.getAttribute('data-target') || 'body');
      if (target) target.dataset.state = setter.getAttribute('data-set-state');
    }
  });

  document.addEventListener('keydown', function (event) {
    if (event.target.matches('input, textarea, select, [contenteditable]')) return;
    if (event.key === 't' && !event.metaKey && !event.ctrlKey && !event.altKey) applyTheme(currentTheme() === 'dark' ? 'light' : 'dark', true);
    if (event.key === 'Escape') {
      var open = document.querySelector('[data-dismiss-on-escape]');
      if (open) open.hidden = true;
    }
  });

  // Carry the current theme across sibling-state links so a review stays consistent.
  document.querySelectorAll('.mock-bar a[href]').forEach(function (link) {
    link.addEventListener('click', function () {
      try {
        var url = new URL(link.href);
        url.searchParams.set('theme', currentTheme());
        if (root.dataset.chrome === 'off') url.searchParams.set('chrome', '0');
        link.href = url.toString();
      } catch (e) { /* relative file URLs in some viewers */ }
    });
  });
})();

/* Tulle field: the marketing hero's canvas of drifting tulle petals and sparkles.
   The pointer twirls the petals around it; a tap throws a burst of sparkles.
   Colours come from the canvas's CSS custom properties so light and dark themes follow
   Giddy's tokens. prefers-reduced-motion paints one still frame; offscreen, it sleeps. */
(function () {
  var canvases = document.querySelectorAll('canvas[data-tulle]');
  if (!canvases.length) return;
  var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  var TAU = Math.PI * 2;

  canvases.forEach(function (canvas) {
    var ctx = canvas.getContext('2d');
    if (!ctx) return;
    var host = canvas.parentElement;
    var w = 0, h = 0, dpr = 1, palette = [], petals = [], sparks = [], visible = true, frame = 0;
    var pointer = { x: -9999, y: -9999, active: false };

    function readPalette() {
      var style = getComputedStyle(canvas);
      palette = ['--tulle-a', '--tulle-b', '--tulle-c', '--tulle-d'].map(function (name) {
        return style.getPropertyValue(name).trim() || '#e85a8f';
      });
    }

    function petal(randomY) {
      var size = 10 + Math.random() * 26;
      return {
        x: Math.random() * w, y: randomY ? Math.random() * h : h + size * 2,
        size: size, depth: 0.4 + Math.random() * 0.6,
        angle: Math.random() * TAU, spin: (Math.random() - 0.5) * 0.02,
        sway: Math.random() * TAU, vx: 0, vy: 0,
        colour: palette[Math.floor(Math.random() * palette.length)], frills: 5 + Math.floor(Math.random() * 3)
      };
    }

    function resize() {
      var rect = host.getBoundingClientRect();
      dpr = Math.min(window.devicePixelRatio || 1, 2);
      w = rect.width; h = rect.height;
      canvas.width = Math.round(w * dpr); canvas.height = Math.round(h * dpr);
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      var count = Math.round(Math.min(70, Math.max(24, (w * h) / 16000)));
      petals = [];
      for (var i = 0; i < count; i++) petals.push(petal(true));
    }

    // A tulle petal: a scalloped fan, like one layer of a tutu seen from above.
    function drawPetal(p) {
      ctx.save();
      ctx.translate(p.x, p.y);
      ctx.rotate(p.angle);
      ctx.globalAlpha = 0.18 + p.depth * 0.32;
      ctx.fillStyle = p.colour;
      ctx.beginPath();
      ctx.moveTo(0, 0);
      var spread = Math.PI * 0.9, r = p.size;
      for (var i = 0; i <= p.frills; i++) {
        var a0 = -spread / 2 + (spread * i) / p.frills;
        var a1 = a0 + spread / p.frills / 2;
        if (i === 0) ctx.lineTo(Math.cos(a0) * r, Math.sin(a0) * r);
        else ctx.quadraticCurveTo(Math.cos(a1 - spread / p.frills) * r * 1.22, Math.sin(a1 - spread / p.frills) * r * 1.22, Math.cos(a0) * r, Math.sin(a0) * r);
      }
      ctx.closePath();
      ctx.fill();
      ctx.globalAlpha *= 0.7;
      ctx.strokeStyle = p.colour;
      ctx.lineWidth = 0.75;
      ctx.stroke();
      ctx.restore();
    }

    function drawSpark(s) {
      var r = s.size * (s.life / s.max);
      ctx.save();
      ctx.translate(s.x, s.y);
      ctx.rotate(s.angle);
      ctx.globalAlpha = Math.min(1, s.life / 20);
      ctx.fillStyle = s.colour;
      ctx.beginPath();
      for (var i = 0; i < 8; i++) {
        var rr = i % 2 ? r * 0.28 : r;
        ctx.lineTo(Math.cos((i * TAU) / 8) * rr, Math.sin((i * TAU) / 8) * rr);
      }
      ctx.closePath();
      ctx.fill();
      ctx.restore();
    }

    function burst(x, y, n) {
      for (var i = 0; i < n; i++) {
        var a = Math.random() * TAU, v = 1.5 + Math.random() * 4;
        sparks.push({ x: x, y: y, vx: Math.cos(a) * v, vy: Math.sin(a) * v - 1.5, angle: a, size: 4 + Math.random() * 7,
                      life: 50 + Math.random() * 30, max: 80, colour: palette[i % palette.length] });
      }
    }

    function step(t) {
      ctx.clearRect(0, 0, w, h);
      petals.forEach(function (p, i) {
        // Twirl: a tangential pull around the pointer, strongest close in.
        if (pointer.active) {
          var dx = p.x - pointer.x, dy = p.y - pointer.y, d = Math.sqrt(dx * dx + dy * dy) || 1;
          if (d < 220) {
            var f = (1 - d / 220) * 0.9 * p.depth;
            p.vx += (-dy / d) * f + (dx / d) * f * 0.25;
            p.vy += (dx / d) * f + (dy / d) * f * 0.25;
            p.spin += 0.002 * f;
          }
        }
        p.vx *= 0.94; p.vy *= 0.94; p.spin *= 0.995;
        p.sway += 0.012;
        p.x += p.vx + Math.sin(p.sway + t * 0.0004) * 0.35 * p.depth;
        p.y += p.vy - (0.25 + p.depth * 0.45);
        p.angle += p.spin + 0.003;
        if (p.y < -p.size * 2 || p.x < -80 || p.x > w + 80) petals[i] = petal(false);
        drawPetal(p);
      });
      sparks = sparks.filter(function (s) {
        s.vx *= 0.96; s.vy = s.vy * 0.96 + 0.06; s.x += s.vx; s.y += s.vy; s.angle += 0.08; s.life -= 1;
        if (s.life > 0) drawSpark(s);
        return s.life > 0;
      });
    }

    function loop(t) {
      frame = 0;
      if (!visible) return;
      step(t);
      frame = requestAnimationFrame(loop);
    }

    function start() { if (!reduce && !frame) frame = requestAnimationFrame(loop); }

    readPalette();
    resize();
    if (reduce) step(0); else start();

    window.addEventListener('resize', function () { resize(); if (reduce) step(0); });
    new MutationObserver(function () { readPalette(); petals.forEach(function (p) { p.colour = palette[Math.floor(Math.random() * palette.length)]; }); if (reduce) step(0); })
      .observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
    if ('IntersectionObserver' in window) {
      new IntersectionObserver(function (entries) { visible = entries[0].isIntersecting; if (visible) start(); }).observe(host);
    }
    if (reduce) return;
    host.addEventListener('pointermove', function (e) {
      var rect = host.getBoundingClientRect();
      pointer.x = e.clientX - rect.left; pointer.y = e.clientY - rect.top; pointer.active = true;
    });
    host.addEventListener('pointerleave', function () { pointer.active = false; });
    host.addEventListener('pointerdown', function (e) {
      var rect = host.getBoundingClientRect();
      burst(e.clientX - rect.left, e.clientY - rect.top, e.target.closest('a, button') ? 28 : 18);
    });
  });
})();

/* What's her closet worth? Chips toggle aria-pressed; the total counts up to the new sum. */
(function () {
  var section = document.querySelector('.worth');
  if (!section) return;
  var totalEl = section.querySelector('[data-worth-total]');
  var countEl = section.querySelector('[data-worth-count]');
  var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  var shown = parseInt(totalEl.textContent.replace(/\D/g, ''), 10) || 0, raf = 0;

  function render(target) {
    cancelAnimationFrame(raf);
    var from = shown, start = performance.now(), ms = reduce ? 0 : 450;
    (function tick(now) {
      var t = ms ? Math.min(1, (now - start) / ms) : 1;
      shown = Math.round(from + (target - from) * (1 - Math.pow(1 - t, 3)));
      totalEl.textContent = '$' + shown;
      if (t < 1) raf = requestAnimationFrame(tick);
    })(start);
    var box = totalEl.parentElement;
    box.classList.remove('is-bumped'); void box.offsetWidth; box.classList.add('is-bumped');
  }

  section.addEventListener('click', function (event) {
    var chip = event.target.closest('.worth__chip');
    if (!chip) return;
    chip.setAttribute('aria-pressed', chip.getAttribute('aria-pressed') === 'true' ? 'false' : 'true');
    var on = section.querySelectorAll('.worth__chip[aria-pressed="true"]'), sum = 0;
    on.forEach(function (c) { sum += parseInt(c.dataset.worth, 10); });
    countEl.textContent = on.length;
    render(sum);
  });
})();
