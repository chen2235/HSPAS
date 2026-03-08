// 功能選單排序管理
HSPAS.registerPage('settings/menu-sorting', async function () {
    const container = document.getElementById('menuTreeContainer');
    const nodeInfoPanel = document.getElementById('nodeInfoPanel');
    const msgDiv = document.getElementById('menuSortMsg');
    let menuTree = [];
    let selectedNode = null;

    function showMsg(type, text) {
        const ts = new Date().toLocaleTimeString('zh-TW');
        msgDiv.innerHTML = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
            <small class="text-muted me-2">[${ts}]</small>${text}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>`;
        msgDiv.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        if (type === 'success') setTimeout(() => { msgDiv.innerHTML = ''; }, 3000);
    }

    // 載入選單樹
    async function loadTree() {
        try {
            const resp = await fetch('/api/menu/tree');
            if (!resp.ok) throw new Error('API 錯誤');
            menuTree = await resp.json();
            renderTree();
        } catch (e) {
            container.innerHTML = '<div class="alert alert-danger m-3">載入選單失敗：' + e.message + '</div>';
        }
    }

    // 渲染樹狀結構
    function renderTree() {
        container.innerHTML = buildTreeHtml(menuTree);
        bindDragEvents();
        bindClickEvents();
    }

    function buildTreeHtml(nodes, depth = 0) {
        if (!nodes || nodes.length === 0) return '';
        let html = `<ul class="menu-tree-list" data-depth="${depth}">`;
        for (const node of nodes) {
            const levelClass = node.level === 1 ? 'l1' : node.level === 2 ? 'l2' : 'l3';
            const hasChildren = node.children && node.children.length > 0;
            const toggleIcon = hasChildren ? '<i class="bi bi-chevron-down"></i>' : '<i class="bi bi-dot"></i>';
            html += `<li class="menu-tree-item" data-id="${node.id}" data-level="${node.level}" data-parent-id="${node.parentId || ''}">
                <div class="menu-tree-item-content" draggable="true" data-id="${node.id}">
                    <span class="drag-handle"><i class="bi bi-grip-vertical"></i></span>
                    <span class="toggle-btn">${toggleIcon}</span>
                    <span class="level-badge ${levelClass}">L${node.level}</span>
                    <span class="display-name">${node.displayName}</span>
                    <span class="func-code">${node.funcCode}</span>
                </div>`;
            if (hasChildren) {
                html += buildTreeHtml(node.children, depth + 1);
            }
            html += '</li>';
        }
        html += '</ul>';
        return html;
    }

    // 拖拉排序事件
    let draggedItem = null;

    function bindDragEvents() {
        container.querySelectorAll('.menu-tree-item-content[draggable="true"]').forEach(el => {
            el.addEventListener('dragstart', (e) => {
                draggedItem = el.closest('.menu-tree-item');
                e.dataTransfer.effectAllowed = 'move';
                e.dataTransfer.setData('text/plain', el.dataset.id);
                setTimeout(() => { draggedItem.style.opacity = '0.4'; }, 0);
            });

            el.addEventListener('dragend', () => {
                if (draggedItem) draggedItem.style.opacity = '1';
                draggedItem = null;
                container.querySelectorAll('.drag-over').forEach(d => d.classList.remove('drag-over'));
                container.querySelectorAll('.drag-over-child').forEach(d => d.classList.remove('drag-over-child'));
            });

            el.addEventListener('dragover', (e) => {
                e.preventDefault();
                e.dataTransfer.dropEffect = 'move';
                const target = el.closest('.menu-tree-item');
                if (target !== draggedItem) {
                    container.querySelectorAll('.drag-over').forEach(d => d.classList.remove('drag-over'));
                    container.querySelectorAll('.drag-over-child').forEach(d => d.classList.remove('drag-over-child'));

                    const draggedLevel = parseInt(draggedItem.dataset.level);
                    const targetLevel = parseInt(target.dataset.level);

                    // 拖到同層 → 排序指示線；拖到上一層 → 變成其子節點指示
                    if (draggedLevel === targetLevel && draggedItem.parentElement === target.parentElement) {
                        el.classList.add('drag-over');
                    } else if (draggedLevel === targetLevel + 1) {
                        el.classList.add('drag-over-child');
                    } else if (draggedLevel === targetLevel) {
                        el.classList.add('drag-over');
                    }
                }
            });

            el.addEventListener('dragleave', () => {
                el.classList.remove('drag-over');
                el.classList.remove('drag-over-child');
            });

            el.addEventListener('drop', (e) => {
                e.preventDefault();
                el.classList.remove('drag-over');
                el.classList.remove('drag-over-child');
                const targetItem = el.closest('.menu-tree-item');
                if (!draggedItem || targetItem === draggedItem) return;

                const draggedLevel = parseInt(draggedItem.dataset.level);
                const targetLevel = parseInt(targetItem.dataset.level);

                if (draggedLevel === targetLevel && draggedItem.parentElement === targetItem.parentElement) {
                    // 同層級同父節點：排序調整
                    targetItem.parentElement.insertBefore(draggedItem, targetItem);
                    updateTreeFromDom();
                    showMsg('info', '已調整排序位置，請按「儲存排序」以確認變更。');
                } else if (draggedLevel === targetLevel + 1) {
                    // 跨層級：拖到上一層節點 → 變成該節點的子節點
                    let childList = targetItem.querySelector(':scope > .menu-tree-list');
                    if (!childList) {
                        // 目標節點還沒有子清單，建立一個
                        childList = document.createElement('ul');
                        childList.className = 'menu-tree-list';
                        childList.dataset.depth = String(targetLevel);
                        targetItem.appendChild(childList);
                        // 更新 toggle icon
                        const toggle = targetItem.querySelector('.toggle-btn i');
                        if (toggle) toggle.className = 'bi bi-chevron-down';
                    }
                    childList.appendChild(draggedItem);
                    // 更新拖動節點的 parentId 與 level
                    const targetId = targetItem.dataset.id;
                    draggedItem.dataset.parentId = targetId;
                    updateTreeData(parseInt(draggedItem.dataset.id), parseInt(targetId), draggedLevel);
                    updateTreeFromDom();
                    showMsg('info', `已將節點移動至「${targetItem.querySelector('.display-name').textContent}」底下，請按「儲存排序」以確認變更。`);
                } else if (draggedLevel === targetLevel) {
                    // 同層級但不同父節點：移到目標節點的同一父清單
                    targetItem.parentElement.insertBefore(draggedItem, targetItem);
                    const newParentId = targetItem.dataset.parentId;
                    draggedItem.dataset.parentId = newParentId;
                    updateTreeData(parseInt(draggedItem.dataset.id), newParentId ? parseInt(newParentId) : null, draggedLevel);
                    updateTreeFromDom();
                    showMsg('info', '已調整排序位置，請按「儲存排序」以確認變更。');
                } else {
                    showMsg('warning', '不支援此移動操作（僅可在同層排序，或移到上一層節點底下）。');
                }
            });
        });
    }

    // 更新記憶體中的 tree data（parentId 與子節點位置）
    function updateTreeData(nodeId, newParentId, level) {
        // 從舊位置移除
        function removeFromTree(nodes) {
            for (let i = 0; i < nodes.length; i++) {
                if (nodes[i].id === nodeId) {
                    return nodes.splice(i, 1)[0];
                }
                if (nodes[i].children) {
                    const found = removeFromTree(nodes[i].children);
                    if (found) return found;
                }
            }
            return null;
        }
        const node = removeFromTree(menuTree);
        if (!node) return;

        node.parentId = newParentId;
        node.level = level;

        // 插入到新位置
        if (newParentId === null) {
            menuTree.push(node);
        } else {
            const parent = findNode(menuTree, newParentId);
            if (parent) {
                if (!parent.children) parent.children = [];
                parent.children.push(node);
            }
        }
    }

    // 點選節點顯示資訊
    function bindClickEvents() {
        container.querySelectorAll('.menu-tree-item-content').forEach(el => {
            el.addEventListener('click', (e) => {
                // 如果點擊的是 toggle-btn，則收合/展開
                if (e.target.closest('.toggle-btn')) {
                    const li = el.closest('.menu-tree-item');
                    const childList = li.querySelector(':scope > .menu-tree-list');
                    if (childList) {
                        childList.style.display = childList.style.display === 'none' ? '' : 'none';
                        const toggle = el.querySelector('.toggle-btn i');
                        toggle.className = childList.style.display === 'none' ? 'bi bi-chevron-right' : 'bi bi-chevron-down';
                    }
                    return;
                }

                // 選取節點
                container.querySelectorAll('.menu-tree-item-content.selected').forEach(s => s.classList.remove('selected'));
                el.classList.add('selected');

                const nodeId = parseInt(el.dataset.id);
                const node = findNode(menuTree, nodeId);
                if (node) {
                    selectedNode = node;
                    showNodeInfo(node);
                }
            });
        });
    }

    function findNode(nodes, id) {
        for (const n of nodes) {
            if (n.id === id) return n;
            if (n.children) {
                const found = findNode(n.children, id);
                if (found) return found;
            }
        }
        return null;
    }

    function showNodeInfo(node) {
        nodeInfoPanel.innerHTML = `
            <div class="node-info-row"><span class="label">ID</span><span>${node.id}</span></div>
            <div class="node-info-row"><span class="label">功能代碼</span><span><code>${node.funcCode}</code></span></div>
            <div class="node-info-row"><span class="label">顯示名稱</span><span>${node.displayName}</span></div>
            <div class="node-info-row"><span class="label">層級</span><span>Level ${node.level}</span></div>
            <div class="node-info-row"><span class="label">父節點 ID</span><span>${node.parentId ?? '(根節點)'}</span></div>
            <div class="node-info-row"><span class="label">排序</span><span>${node.sortOrder}</span></div>
            <div class="node-info-row"><span class="label">路由 URL</span><span>${node.routeUrl || '(無)'}</span></div>
            <div class="node-info-row"><span class="label">啟用</span><span>${node.isActive ? '<span class="badge bg-success">是</span>' : '<span class="badge bg-secondary">否</span>'}</span></div>
            <div class="node-info-row"><span class="label">子節點數</span><span>${(node.children || []).length}</span></div>
        `;
    }

    // 從 DOM 結構重建 sortOrder
    function updateTreeFromDom() {
        function processChildren(ulElement) {
            const items = ulElement.querySelectorAll(':scope > .menu-tree-item');
            items.forEach((li, idx) => {
                const id = parseInt(li.dataset.id);
                const node = findNode(menuTree, id);
                if (node) {
                    node.sortOrder = idx + 1;
                }
            });
        }
        container.querySelectorAll('.menu-tree-list').forEach(ul => processChildren(ul));
    }

    // 收集所有節點（扁平化）
    function flattenTree(nodes) {
        const result = [];
        for (const n of nodes) {
            result.push({ id: n.id, parentId: n.parentId, level: n.level, sortOrder: n.sortOrder });
            if (n.children) result.push(...flattenTree(n.children));
        }
        return result;
    }

    // 儲存排序
    document.getElementById('btnSaveOrder').addEventListener('click', async () => {
        updateTreeFromDom();
        const items = flattenTree(menuTree);

        try {
            const resp = await fetch('/api/menu/reorder', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(items)
            });
            const data = await resp.json();
            if (resp.ok && data.success) {
                showMsg('success', '選單排序已儲存成功！');
                // 重新載入 sidebar
                if (window.HSPAS.reloadSidebar) {
                    await window.HSPAS.reloadSidebar();
                }
                await loadTree();
            } else {
                showMsg('danger', '儲存失敗：' + (data.error || '未知錯誤'));
            }
        } catch (e) {
            showMsg('danger', '儲存失敗：' + e.message);
        }
    });

    // 全部展開
    document.getElementById('btnExpandAll').addEventListener('click', () => {
        container.querySelectorAll('.menu-tree-list').forEach(ul => { ul.style.display = ''; });
        container.querySelectorAll('.toggle-btn i').forEach(i => {
            if (i.className.includes('chevron')) i.className = 'bi bi-chevron-down';
        });
    });

    // 全部收合
    document.getElementById('btnCollapseAll').addEventListener('click', () => {
        container.querySelectorAll('.menu-tree-list .menu-tree-list').forEach(ul => { ul.style.display = 'none'; });
        container.querySelectorAll('.menu-tree-list .toggle-btn i').forEach(i => {
            if (i.className.includes('chevron')) i.className = 'bi bi-chevron-right';
        });
    });

    await loadTree();
});
