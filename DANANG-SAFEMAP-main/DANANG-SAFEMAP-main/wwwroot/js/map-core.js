// ═══════════════════════════════════════════════════════════
// MAP-CORE.JS — Khởi tạo bản đồ + markers + layers
// ═══════════════════════════════════════════════════════════

const MapCore = {
    map: null,
    markerLayer: null,
    clusterGroup: null,
    heatLayer: null,
    currentAlerts: [],
    timeRanges: [24, 48, 168, 720], // giờ
    currentTimeRange: 24,

    // ── Khởi tạo ──
    init() {
        // Tạo map center Đà Nẵng — zoom 14 vừa trang thành phố
        this.map = L.map('map', {
            center: [16.054407, 108.202164],
            zoom: 13,
            zoomControl: false,
            // Giới hạn vùng: chỉ khu vực Đà Nẵng
            maxBounds: [[15.97, 107.98], [16.18, 108.42]],
            maxBoundsViscosity: 0.9,
            minZoom: 11,
            maxZoom: 19
        });

        // Zoom control góc phải
        L.control.zoom({ position: 'topright' }).addTo(this.map);

        // Tile layer — Dark style
        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OSM</a> | <a href="https://carto.com/">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 19
        }).addTo(this.map);

        // Tạo 3 layer groups
        this.markerLayer = L.layerGroup().addTo(this.map);
        this.clusterGroup = L.markerClusterGroup({
            maxClusterRadius: 50,
            spiderfyOnMaxZoom: true,
            showCoverageOnHover: false,
            iconCreateFunction: (cluster) => {
                const count = cluster.getChildCount();
                let size = 'small';
                if (count >= 10) size = 'medium';
                if (count >= 30) size = 'large';
                return L.divIcon({
                    html: `<div class="cluster-icon cluster-${size}"><span>${count}</span></div>`,
                    className: 'custom-cluster',
                    iconSize: [40, 40]
                });
            }
        });
        this.heatLayer = L.heatLayer([], {
            radius: 30,
            blur: 20,
            maxZoom: 14,
            max: 1.0,
            gradient: {
                0.2: '#2563EB',
                0.4: '#10B981',
                0.6: '#F59E0B',
                0.8: '#EF4444',
                1.0: '#DC2626'
            }
        });

        // Bind events
        this._bindEvents();

        // Init report form
        ReportForm.init(this.map);

        // Guide button
        document.getElementById('btn-guide').addEventListener('click', () => {
            document.getElementById('guide-modal').style.display = 'flex';
        });

        // Đóng guide khi click overlay
        document.getElementById('guide-modal').addEventListener('click', function(e) {
            if (e.target === this) this.style.display = 'none';
        });

        // Load data
        this._initTimeSlider();
        this._initFilters();
        this.loadAlerts();
        this._initLocateButton();
    },

    // ── Events ──
    _bindEvents() {
        // Load lại khi di chuyển bản đồ
        let moveTimeout;
        this.map.on('moveend', () => {
            clearTimeout(moveTimeout);
            moveTimeout = setTimeout(() => this.loadAlerts(), 300);
        });

        // Đổi layer theo zoom
        this.map.on('zoomend', () => {
            this._updateLayerVisibility();
        });
    },

    // ── Đổi layer theo zoom ──
    _updateLayerVisibility() {
        const zoom = this.map.getZoom();

        if (zoom <= 12) {
            // Heatmap
            this.map.removeLayer(this.markerLayer);
            this.map.removeLayer(this.clusterGroup);
            if (!this.map.hasLayer(this.heatLayer)) {
                this.map.addLayer(this.heatLayer);
            }
            this._loadHeatmap();
        } else if (zoom <= 15) {
            // Cluster
            this.map.removeLayer(this.heatLayer);
            this.map.removeLayer(this.markerLayer);
            if (!this.map.hasLayer(this.clusterGroup)) {
                this.map.addLayer(this.clusterGroup);
            }
        } else {
            // Individual markers
            this.map.removeLayer(this.heatLayer);
            this.map.removeLayer(this.clusterGroup);
            if (!this.map.hasLayer(this.markerLayer)) {
                this.map.addLayer(this.markerLayer);
            }
        }
    },

    // ── Load markers ──
    async loadAlerts() {
        try {
            const bounds = this.map.getBounds();
            const now = new Date();
            const from = new Date(now.getTime() - this.currentTimeRange * 3600000);

            const alerts = await MapAPI.getMapAlerts(bounds, from, now);
            this.currentAlerts = alerts;

            this._renderMarkers(alerts);
        } catch (e) {
            console.error('Lỗi tải dữ liệu bản đồ:', e);
        }
    },

    _renderMarkers(alerts) {
        this.markerLayer.clearLayers();
        this.clusterGroup.clearLayers();

        alerts.forEach(alert => {
            const marker = this._createMarker(alert);

            // Add to both layers (switch visibility by zoom)
            this.markerLayer.addLayer(marker);
            this.clusterGroup.addLayer(this._createMarker(alert));
        });

        this._updateLayerVisibility();
    },

    _createMarker(alert) {
        const opacity = Math.max(0.3, alert.opacity / 100);
        const color = alert.categoryColor || '#666';

        const icon = L.divIcon({
            className: 'custom-marker-icon',
            html: `<div class="marker-icon" style="background:${color};opacity:${opacity}">
                     <span>${alert.iconEmoji || '⚠️'}</span>
                   </div>`,
            iconSize: [36, 36],
            iconAnchor: [18, 36],
            popupAnchor: [0, -36]
        });

        const marker = L.marker([alert.latitude, alert.longitude], { icon });

        // Build popup
        marker.on('click', async () => {
            const detail = await MapAPI.getAlertDetail(alert.id);
            marker.bindPopup(this._buildPopup(detail), {
                maxWidth: 320,
                minWidth: 280,
                closeButton: true
            }).openPopup();
        });

        return marker;
    },

    _buildPopup(a) {
        const timeAgo = this._timeAgo(new Date(a.incidentTime));
        const mediaHtml = a.mediaUrls && a.mediaUrls.length > 0
            ? `<div class="popup-media">${a.mediaUrls.map(url => `<img src="${url}" alt="Media" onclick="window.open('${url}','_blank')" />`).join('')}</div>`
            : '';

        const statusLabel = {
            'VISIBLE_VERIFIED': '✅ Đã xác thực',
            'VISIBLE_UNVERIFIED': '⏳ Chưa xác thực',
            'PENDING_REVIEW': '🔍 Chờ duyệt',
            'RESOLVED': '✔️ Đã xử lý'
        }[a.status] || a.status;

        return `
            <div class="popup-content">
                <div class="popup-header">
                    <span class="popup-emoji">${a.iconEmoji || '⚠️'}</span>
                    <div>
                        <div class="popup-title">${this._escHtml(a.title)}</div>
                        <div style="font-size:11px;color:var(--map-text-muted);margin-top:2px">${a.alertTypeName}</div>
                    </div>
                </div>
                <div class="popup-meta">
                    <span>📍 ${this._escHtml(a.addressText || 'Không có địa chỉ')}</span>
                    <span>🕐 ${timeAgo} — ${statusLabel}</span>
                    <span>👤 ${this._escHtml(a.userName)}</span>
                </div>
                <div class="popup-desc">${this._escHtml(a.description)}</div>
                ${mediaHtml}
                <div class="popup-stats">
                    <div class="popup-stat popup-stat--confirm">👍 ${a.confirmCount}</div>
                    <div class="popup-stat popup-stat--deny">👎 ${a.denyCount}</div>
                </div>
                <div class="popup-actions">
                    <button class="popup-btn popup-btn--confirm" onclick="MapCore.verifyAlert(${a.id},'CONFIRM')">
                        👍 Tôi cũng ghi nhận
                    </button>
                    <button class="popup-btn popup-btn--deny" onclick="MapCore.verifyAlert(${a.id},'DENY')">
                        👎 Không ghi nhận
                    </button>
                </div>
            </div>
        `;
    },

    // ── Xác nhận cộng đồng ──
    async verifyAlert(alertId, type) {
        const token = localStorage.getItem('token');
        if (!token) {
            showToast('Vui lòng đăng nhập để xác nhận', 'error');
            return;
        }

        try {
            const result = await MapAPI.verifyAlert(alertId, {
                verificationType: type,
                latitude: this.map.getCenter().lat,
                longitude: this.map.getCenter().lng
            });

            if (result.success) {
                showToast(result.message, 'success');
                this.loadAlerts(); // Reload markers
            } else {
                showToast(result.message, 'error');
            }
        } catch (e) {
            showToast('Lỗi kết nối', 'error');
        }
    },

    // ── Heatmap ──
    async _loadHeatmap() {
        try {
            const now = new Date();
            const from = new Date(now.getTime() - this.currentTimeRange * 3600000);
            const data = await MapAPI.getHeatmapData(from, now);
            const points = data.map(d => [d.lat, d.lng, d.intensity || 1]);
            this.heatLayer.setLatLngs(points);
        } catch (e) {
            console.error('Lỗi tải heatmap:', e);
        }
    },

    // ── Time Slider ──
    _initTimeSlider() {
        const slider = document.getElementById('timeSlider');
        const label = document.getElementById('sliderLabel');
        const labels = ['24 giờ qua', '48 giờ qua', '7 ngày qua', '30 ngày qua'];

        slider.addEventListener('input', () => {
            const idx = parseInt(slider.value);
            label.textContent = labels[idx];
            this.currentTimeRange = this.timeRanges[idx];
            this.loadAlerts();
        });
    },

    // ── Bộ lọc loại sự cố ──
    async _initFilters() {
        try {
            const types = await MapAPI.getAlertTypes();
            const container = document.getElementById('filter-types');

            // All button
            const allChip = document.createElement('button');
            allChip.className = 'filter-chip active';
            allChip.textContent = 'Tất cả';
            allChip.dataset.typeId = 'all';
            allChip.addEventListener('click', () => this._filterByType('all'));
            container.appendChild(allChip);

            types.forEach(t => {
                const chip = document.createElement('button');
                chip.className = 'filter-chip';
                chip.dataset.typeId = t.id;
                chip.innerHTML = `<span class="chip-dot" style="background:${t.categoryColor}"></span>${t.iconEmoji || ''} ${t.name}`;
                chip.addEventListener('click', () => this._filterByType(t.id));
                container.appendChild(chip);
            });
        } catch (e) {
            console.error('Lỗi tải bộ lọc:', e);
        }
    },

    _filterByType(typeId) {
        // Update UI
        document.querySelectorAll('.filter-chip').forEach(c => c.classList.remove('active'));
        document.querySelector(`.filter-chip[data-type-id="${typeId}"]`).classList.add('active');

        // Lọc markers
        if (typeId === 'all') {
            this._renderMarkers(this.currentAlerts);
        } else {
            const filtered = this.currentAlerts.filter(a => a.alertTypeId === typeId);
            this._renderMarkers(filtered);
        }
    },

    // ── Nút định vị ──
    _initLocateButton() {
        document.getElementById('btn-locate').addEventListener('click', () => {
            if (navigator.geolocation) {
                navigator.geolocation.getCurrentPosition(
                    (pos) => {
                        const { latitude, longitude } = pos.coords;
                        this.map.setView([latitude, longitude], 16);
                        L.marker([latitude, longitude], {
                            icon: L.divIcon({
                                className: 'user-location',
                                html: '<div style="width:14px;height:14px;background:#3B82F6;border-radius:50%;border:3px solid #fff;box-shadow:0 0 12px rgba(59,130,246,0.6)"></div>',
                                iconSize: [14, 14],
                                iconAnchor: [7, 7]
                            })
                        }).addTo(this.map);
                    },
                    () => showToast('Không thể xác định vị trí của bạn', 'error')
                );
            }
        });
    },

    // ── Helpers ──
    _timeAgo(date) {
        const diff = (Date.now() - date.getTime()) / 1000;
        if (diff < 60) return 'Vừa xong';
        if (diff < 3600) return `${Math.floor(diff / 60)} phút trước`;
        if (diff < 86400) return `${Math.floor(diff / 3600)} giờ trước`;
        return `${Math.floor(diff / 86400)} ngày trước`;
    },

    _escHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }
};

// ── Cluster icon CSS (inject) ──
(function() {
    const style = document.createElement('style');
    style.textContent = `
        .custom-cluster { background: none !important; border: none !important; }
        .cluster-icon {
            display: flex; align-items: center; justify-content: center;
            width: 40px; height: 40px; border-radius: 50%;
            font-weight: 700; font-size: 14px; color: #fff;
            box-shadow: 0 2px 12px rgba(0,0,0,0.3);
        }
        .cluster-small { background: rgba(16,185,129,0.85); }
        .cluster-medium { background: rgba(245,158,11,0.85); width: 46px; height: 46px; font-size: 15px; }
        .cluster-large { background: rgba(239,68,68,0.85); width: 54px; height: 54px; font-size: 16px; }
    `;
    document.head.appendChild(style);
})();

// ── Start! ──
document.addEventListener('DOMContentLoaded', () => MapCore.init());
