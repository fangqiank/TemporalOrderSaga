const API = '/api';
let products = [];
let cart = JSON.parse(localStorage.getItem('cart') || '[]');

const productEmojis = {
    '键盘': '&#9000;', '鼠标': '&#128433;', '显示器': '&#128187;',
    '音频': '&#127925;', '配件': '&#128268;'
};

// ---- Init ----
document.addEventListener('DOMContentLoaded', () => {
    fetchProducts();
    renderCart();
});

// ---- Products ----
async function fetchProducts() {
    const res = await fetch(`${API}/products`);
    products = await res.json();
    renderProducts();
}

function renderProducts(category = 'all') {
    const grid = document.getElementById('productGrid');
    const filtered = category === 'all' ? products : products.filter(p => p.category === category);

    grid.innerHTML = filtered.map(p => `
        <div class="product-card">
            <div class="product-img">${productEmojis[p.category] || '&#128722;'}</div>
            <div class="product-info">
                <div class="product-name">${p.name}</div>
                <div class="product-desc">${p.description}</div>
                <div class="product-meta">
                    <span class="product-price">&yen;${p.price.toFixed(2)}</span>
                    <span class="product-stock">${p.stock > 0 ? `库存 ${p.stock}` : '缺货'}</span>
                </div>
                <div style="margin-top:12px">
                    <button class="btn btn-primary btn-sm" ${p.stock === 0 ? 'disabled' : ''}
                        onclick="addToCart('${p.id}')">加入购物车</button>
                </div>
            </div>
        </div>
    `).join('');
}

function filterCategory(cat) {
    document.querySelectorAll('.filter-btn').forEach(b => b.classList.toggle('active', b.textContent.trim() === cat || cat === 'all' && b.textContent.trim() === '全部'));
    renderProducts(cat);
}

// ---- Cart ----
function addToCart(productId) {
    const product = products.find(p => p.id === productId);
    if (!product) return;

    const existing = cart.find(c => c.productId === productId);
    if (existing) {
        if (existing.quantity >= product.stock) {
            showToast('超出库存数量', 'error');
            return;
        }
        existing.quantity++;
    } else {
        cart.push({ productId, name: product.name, price: product.price, quantity: 1, emoji: productEmojis[product.category] || '&#128722;' });
    }
    saveCart();
    renderCart();
    showToast(`已添加: ${product.name}`);
}

function removeFromCart(productId) {
    cart = cart.filter(c => c.productId !== productId);
    saveCart();
    renderCart();
}

function updateQty(productId, delta) {
    const item = cart.find(c => c.productId === productId);
    if (!item) return;
    const product = products.find(p => p.id === productId);
    const newQty = item.quantity + delta;
    if (newQty <= 0) return removeFromCart(productId);
    if (product && newQty > product.stock) { showToast('超出库存数量', 'error'); return; }
    item.quantity = newQty;
    saveCart();
    renderCart();
}

function saveCart() {
    localStorage.setItem('cart', JSON.stringify(cart));
    document.getElementById('cartBadge').textContent = cart.reduce((s, c) => s + c.quantity, 0);
}

function renderCart() {
    const container = document.getElementById('cartItems');
    const empty = document.getElementById('cartEmpty');
    const footer = document.getElementById('cartFooter');
    const badge = document.getElementById('cartBadge');
    const total = cart.reduce((s, c) => s + c.price * c.quantity, 0);
    badge.textContent = cart.reduce((s, c) => s + c.quantity, 0);

    if (cart.length === 0) {
        container.innerHTML = '<div class="cart-empty">购物车是空的</div>';
        footer.style.display = 'none';
        return;
    }
    footer.style.display = 'block';
    document.getElementById('cartTotal').textContent = `¥${total.toFixed(2)}`;

    container.innerHTML = cart.map(c => `
        <div class="cart-item">
            <div class="cart-item-img">${c.emoji}</div>
            <div class="cart-item-info">
                <div class="cart-item-name">${c.name}</div>
                <div class="cart-item-price">¥${(c.price * c.quantity).toFixed(2)}</div>
                <div class="cart-item-qty">
                    <button class="qty-btn" onclick="updateQty('${c.productId}', -1)">-</button>
                    <span>${c.quantity}</span>
                    <button class="qty-btn" onclick="updateQty('${c.productId}', 1)">+</button>
                    <button class="cart-item-remove" onclick="removeFromCart('${c.productId}')">移除</button>
                </div>
            </div>
        </div>
    `).join('');
}

function toggleCart() {
    document.getElementById('cartPanel').classList.toggle('open');
    document.getElementById('cartOverlay').classList.toggle('open');
}

// ---- Order ----
async function placeOrder() {
    if (cart.length === 0) return;
    const btn = document.getElementById('checkoutBtn');
    btn.disabled = true;
    btn.textContent = '提交中...';

    try {
        const body = {
            customerId: null,
            items: cart.map(c => ({ productId: c.productId, quantity: c.quantity }))
        };
        const res = await fetch(`${API}/orders`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.error || '下单失败');
        }

        const data = await res.json();
        lastCartSnapshot = [...cart];
        toggleCart();
        cart = [];
        saveCart();
        renderCart();
        trackOrder(data.workflowId, data.orderId);
    } catch (e) {
        showToast(e.message, 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = '立即下单';
    }
}

let lastCartSnapshot = [];
let lastRunningStatus = '';

function trackOrder(workflowId, orderId) {
    showSection('order');
    document.getElementById('orderIdText').textContent = `订单号: ${orderId}`;
    lastRunningStatus = '';
    resetSteps();

    const poll = async () => {
        try {
            const res = await fetch(`${API}/orders/${encodeURIComponent(workflowId)}`);
            const data = await res.json();

            if (data.isRunning) {
                lastRunningStatus = data.status;
                updateSteps(data.status);
                setTimeout(poll, 1500);
            } else {
                showFinalResult(data.status, data.failureReason);
            }
        } catch {
            setTimeout(poll, 3000);
        }
    };
    poll();
}

const STATUS_ORDER = ['Pending', 'InventoryReserved', 'PaymentAuthorized', 'Completed'];

const FAIL_REASONS = {
    'InventoryReserved': '库存预留失败: 库存不足',
    'PaymentAuthorized': '支付授权失败: 余额不足',
};

const COMPENSATION_MAP = {
    'InventoryReserved': ['释放库存'],
    'PaymentAuthorized': ['取消支付', '释放库存'],
};

function resetSteps() {
    document.querySelectorAll('.step').forEach(s => {
        s.classList.remove('active', 'done', 'failed');
    });
    document.getElementById('orderResult').classList.add('hidden');
    document.getElementById('resultSuccess').classList.add('hidden');
    document.getElementById('resultFailed').classList.add('hidden');
    document.getElementById('retryBtn').style.display = 'none';
    document.getElementById('finalStepIcon').innerHTML = '&#9989;';
    document.getElementById('finalStepLabel').textContent = '订单完成';
    document.getElementById('finalStep').classList.remove('final-success', 'final-failed');
}

function updateSteps(currentStatus) {
    if (currentStatus === 'Failed') {
        const prevStep = inferFailedStep();
        document.querySelectorAll('.step').forEach((s, i) => {
            s.classList.remove('active', 'done', 'failed');
            if (i < prevStep) s.classList.add('done');
            else if (i === prevStep) s.classList.add('failed');
        });
        return;
    }

    const idx = STATUS_ORDER.indexOf(currentStatus);
    document.querySelectorAll('.step').forEach((s, i) => {
        s.classList.remove('active', 'done', 'failed');
        if (i < idx) s.classList.add('done');
        else if (i === idx) s.classList.add('active');
    });
}

function inferFailedStep() {
    const map = { 'Pending': 1, 'InventoryReserved': 2, 'PaymentAuthorized': 3 };
    return (map[lastRunningStatus] || 1) - 1;
}

function showFinalResult(status, failureReason) {
    document.getElementById('orderResult').classList.remove('hidden');
    const finalStep = document.getElementById('finalStep');
    const finalIcon = document.getElementById('finalStepIcon');
    const finalLabel = document.getElementById('finalStepLabel');

    // Clear all step states first
    document.querySelectorAll('.step').forEach(s => s.classList.remove('active'));

    if (status === 'Completed') {
        document.querySelectorAll('.step').forEach(s => { s.classList.remove('failed'); s.classList.add('done'); });
        finalStep.classList.add('done');
        finalIcon.innerHTML = '&#9989;';
        finalLabel.textContent = '订单完成';
        document.getElementById('resultSuccess').classList.remove('hidden');
        showToast('订单已完成!', 'success');
    } else {
        // Failed
        const failedIdx = inferFailedStep();
        document.querySelectorAll('.step').forEach((s, i) => {
            s.classList.remove('active', 'done', 'failed');
            if (i < failedIdx) s.classList.add('done');
            else if (i === failedIdx) s.classList.add('failed');
        });
        finalStep.classList.add('final-failed');
        finalIcon.innerHTML = '&#10060;';
        finalLabel.textContent = '已失败';
        document.getElementById('resultFailed').classList.remove('hidden');
        document.getElementById('failReason').textContent = failureReason || '订单处理失败';
        showCompensationLog(lastRunningStatus);
        document.getElementById('retryBtn').style.display = 'inline-block';
        showToast('订单失败，已执行补偿回滚', 'error');
    }
}

function showCompensationLog(failedAfterStep) {
    const log = document.getElementById('compensationLog');
    const compensations = COMPENSATION_MAP[failedAfterStep] || ['释放库存'];
    log.innerHTML = compensations.map((c, i) =>
        `<div class="compensation-item" style="animation-delay:${i * 0.3}s"><span class="comp-check">&#10003;</span> ${c} 已回滚</div>`
    ).join('');
    document.querySelector('.compensation-header').textContent = `↻ 补偿事务已完成 (${compensations.length} 项)`;
}

function retryOrder() {
    showSection('products');
    if (lastCartSnapshot.length > 0) {
        cart = [...lastCartSnapshot];
        saveCart();
        renderCart();
        toggleCart();
    }
}

// ---- UI Helpers ----
function showSection(name) {
    document.getElementById('productsSection').classList.toggle('hidden', name !== 'products');
    document.getElementById('orderSection').classList.toggle('hidden', name !== 'order');
    document.querySelectorAll('.nav-link').forEach(l => l.classList.toggle('active', l.textContent.trim() === (name === 'products' ? '商品' : '')));
}

function showToast(msg, type = 'success') {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = msg;
    container.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}
